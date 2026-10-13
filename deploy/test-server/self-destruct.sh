#!/usr/bin/env bash
# Removes every trace of the Sahno test deployment from this server, and
# nothing else (docs/TEST_DEPLOYMENT.md §14). For a shared server: if it has
# to be handed back or something goes wrong, this takes Sahno off it.
#
#   sudo bash self-destruct.sh          # dry run: lists what would go, changes nothing
#   sudo bash self-destruct.sh --yes    # does it
#
# Copy it somewhere outside the checkout first if you like
# (cp self-destruct.sh /root/); it deletes the checkout it lives in.
#
# What goes:
#   - Docker: every container, volume (certificates), network and image whose
#     compose project is sahno-test or whose name mentions sahno
#   - the checkout (/srv/sahno-test by default, or wherever the containers
#     were started from), with .env shredded first: it holds the database
#     connection string and the Cloudflare token
#   - the ghcr.io login, if this server logged in to pull private images
#   - site blocks for *sahno* hosts in the server's own Caddy and nginx
#     configs, only when the block serves nothing but Sahno hosts; each file
#     is backed up, validated, and put back if the proxy rejects the change
#   - certbot certificates for *sahno* names
#   - the ufw rules for Sahno's HTTPS port, if nothing else listens on it
#   - systemd units named *sahno*
#
# Anything shared that mentions Sahno but cannot be removed safely (a proxy
# block that also serves another host, a crontab line) is reported for you
# to remove by hand, never edited.
#
# What it cannot reach, and is listed at the end: DNS, Cloudflare token,
# Supabase, Auth0, GHCR packages, EAS.

set -uo pipefail

APPLY=false
for arg in "$@"; do
  case "$arg" in
    --yes) APPLY=true ;;
    -h|--help) sed -n '2,32p' "$0"; exit 0 ;;
    *) echo "Unknown option: $arg (use --yes to apply, nothing for a dry run)" >&2; exit 2 ;;
  esac
done

PROJECT=sahno-test
SAHNO_DIR=${SAHNO_DIR:-/srv/sahno-test}
STAMP=$(date +%Y%m%d-%H%M%S)
MANUAL=()

say()  { printf '\n\033[1m== %s\033[0m\n' "$*"; }
info() { printf '   %s\n' "$*"; }
warn() { printf '   \033[33m! %s\033[0m\n' "$*"; MANUAL+=("$*"); }
run() {
  if $APPLY; then
    info "+ $*"
    "$@" || info "  (failed, continuing)"
  else
    info "would run: $*"
  fi
}
have() { command -v "$1" >/dev/null 2>&1; }

if [[ $EUID -ne 0 ]]; then
  echo "Run as root (sudo bash $0 ${*:-})" >&2
  exit 1
fi

cd /

if $APPLY; then
  echo "Removing Sahno from $(hostname). Ctrl-C within 5 seconds to stop."
  sleep 5
else
  echo "DRY RUN on $(hostname): nothing will be changed. Re-run with --yes to apply."
fi

# --- Find the checkout and read what we need from .env before it goes --------

if have docker; then
  wd=$(docker ps -aq --filter "label=com.docker.compose.project=$PROJECT" \
        | head -n1 | xargs -r docker inspect -f '{{index .Config.Labels "com.docker.compose.project.working_dir"}}' 2>/dev/null)
  if [[ -n "${wd:-}" && "$wd" == */deploy/test-server ]]; then
    SAHNO_DIR=${wd%/deploy/test-server}
  fi
fi

ENV_FILE="$SAHNO_DIR/deploy/test-server/.env"
env_value() { [[ -f "$ENV_FILE" ]] && grep -E "^$1=" "$ENV_FILE" | tail -n1 | cut -d= -f2- | tr -d "\"'" ; }
HTTPS_PORT=$(env_value SAHNO_HTTPS_PORT); HTTPS_PORT=${HTTPS_PORT:-8443}
SAHNO_HOST=$(env_value SAHNO_HOST); SAHNO_HOST=${SAHNO_HOST:-api-test.sahno.app}

say "Checkout: $SAHNO_DIR   host: $SAHNO_HOST   HTTPS port: $HTTPS_PORT"

# --- Docker -------------------------------------------------------------------

say "Docker"
if have docker; then
  mapfile -t containers < <(
    { docker ps -aq --filter "label=com.docker.compose.project=$PROJECT"
      docker ps -a --format '{{.ID}} {{.Names}} {{.Image}}' | grep -i sahno | cut -d' ' -f1
    } | sort -u)
  for c in "${containers[@]}"; do
    info "container $(docker inspect -f '{{.Name}} ({{.Config.Image}})' "$c" 2>/dev/null)"
  done
  ((${#containers[@]})) && run docker rm -f "${containers[@]}"

  mapfile -t volumes < <(
    { docker volume ls -q --filter "label=com.docker.compose.project=$PROJECT"
      docker volume ls -q | grep -i sahno
    } | sort -u)
  for v in "${volumes[@]}"; do info "volume $v"; done
  ((${#volumes[@]})) && run docker volume rm -f "${volumes[@]}"

  # Only networks Sahno created. A shared proxy network it merely joined
  # (compose.shared-proxy.yaml) is external and is left alone.
  mapfile -t networks < <(
    { docker network ls -q --filter "label=com.docker.compose.project=$PROJECT"
      docker network ls --format '{{.ID}} {{.Name}}' | grep -i sahno | cut -d' ' -f1
    } | sort -u)
  for n in "${networks[@]}"; do info "network $(docker network inspect -f '{{.Name}}' "$n" 2>/dev/null)"; done
  ((${#networks[@]})) && run docker network rm "${networks[@]}"

  mapfile -t images < <(docker images --format '{{.Repository}}:{{.Tag}} {{.ID}}' | grep -i sahno | awk '{print $2}' | sort -u)
  for i in "${images[@]}"; do info "image $(docker images --format '{{.Repository}}:{{.Tag}} {{.ID}}' | grep "$i" | cut -d' ' -f1 | paste -sd' ')"; done
  ((${#images[@]})) && run docker rmi -f "${images[@]}"

  ((${#containers[@]} + ${#volumes[@]} + ${#networks[@]} + ${#images[@]})) || info "nothing found"

  # ghcr.io credentials, for root and for whoever ran sudo.
  for home in /root ${SUDO_USER:+$(getent passwd "$SUDO_USER" | cut -d: -f6)}; do
    cfg="$home/.docker/config.json"
    if [[ -f "$cfg" ]] && grep -q '"ghcr.io"' "$cfg"; then
      info "ghcr.io login in $cfg"
      if [[ "$home" == /root ]]; then
        run docker logout ghcr.io
      else
        run sudo -u "$SUDO_USER" docker logout ghcr.io
      fi
    fi
  done
else
  info "docker not installed"
fi

# --- Reverse proxies the server already runs ------------------------------------

# Prints the file with every top-level Caddy site block removed whose
# addresses are all *sahno* names. A block that also serves another host is
# kept (and reported afterwards, since "sahno" is still in the file).
strip_caddy_blocks() {
  awk '
    BEGIN { depth = 0; skip = 0 }
    {
      code = $0; sub(/#.*/, "", code)
      opens = gsub(/\{/, "{", code); closes = gsub(/\}/, "}", code)
      if (!skip && depth == 0 && opens > 0) {
        addr = code; sub(/\{.*/, "", addr)
        n = split(addr, parts, /[ ,\t]+/); count = 0; all = 1
        for (k = 1; k <= n; k++) if (parts[k] != "") { count++; if (tolower(parts[k]) !~ /sahno/) all = 0 }
        if (count > 0 && all) skip = 1
      }
      depth += opens - closes
      if (skip) { if (depth <= 0) { skip = 0; depth = 0 } ; next }
      print
    }' "$1"
}

say "Caddy (the server's own, not Sahno's)"
declare -A caddy_files=()   # host path -> "host" or a container id
for f in /etc/caddy/Caddyfile /etc/caddy/*.caddy /etc/caddy/conf.d/* /etc/caddy/sites/*; do
  [[ -f "$f" ]] && caddy_files["$f"]=host
done
if have docker; then
  while read -r cid; do
    while IFS='|' read -r src dst; do
      if [[ "$dst" == /etc/caddy/Caddyfile && -f "$src" ]]; then caddy_files["$src"]=$cid
      elif [[ "$dst" == /etc/caddy && -d "$src" ]]; then
        for f in "$src"/Caddyfile "$src"/*.caddy; do [[ -f "$f" ]] && caddy_files["$f"]=$cid; done
      fi
    done < <(docker inspect -f '{{range .Mounts}}{{.Source}}|{{.Destination}}{{"\n"}}{{end}}' "$cid")
  done < <(docker ps --format '{{.ID}} {{.Image}} {{.Names}}' | grep -i caddy | grep -vi sahno | cut -d' ' -f1)
fi

found_caddy=false
for f in "${!caddy_files[@]}"; do
  grep -qi sahno "$f" || continue
  found_caddy=true
  where=${caddy_files[$f]}
  new=$(strip_caddy_blocks "$f")
  if [[ "$new" == "$(cat "$f")" ]]; then
    warn "$f mentions sahno but has no Sahno-only site block; edit it by hand: $(grep -ni sahno "$f" | head -n3 | paste -sd' ')"
    continue
  fi
  info "$f ($([[ $where == host ]] && echo 'host Caddy' || echo "container $where")): removing"
  diff <(cat "$f") <(printf '%s\n' "$new") | grep '^<' | sed 's/^/     /'
  $APPLY || continue

  cp -p "$f" "$f.sahno-backup-$STAMP"
  # Rewrite in place (not mv): a single-file bind mount keeps pointing at the
  # old inode and the container would never see the change.
  printf '%s\n' "$new" > "$f"
  if [[ $where == host ]]; then
    validate() { caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile; }
    reload()   { systemctl reload caddy 2>/dev/null || caddy reload --config /etc/caddy/Caddyfile --adapter caddyfile; }
  else
    validate() { docker exec "$where" caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile; }
    reload()   { docker exec "$where" caddy reload --config /etc/caddy/Caddyfile --adapter caddyfile; }
  fi
  if validate >/dev/null 2>&1 && reload >/dev/null 2>&1; then
    info "  validated and reloaded; backup at $f.sahno-backup-$STAMP"
    grep -qi sahno "$f" && warn "$f still mentions sahno outside a Sahno-only block; check it by hand"
  else
    cat "$f.sahno-backup-$STAMP" > "$f"
    warn "Caddy rejected $f without the Sahno block, so it was put back unchanged; edit it by hand"
  fi
done
$found_caddy || info "nothing found"

say "nginx"
if [[ -d /etc/nginx ]]; then
  mapfile -t nginx_files < <(grep -rliI sahno /etc/nginx 2>/dev/null)
  ((${#nginx_files[@]})) || info "nothing found"
  removed_nginx=()
  for f in "${nginx_files[@]}"; do
    names=$(grep -hoE '^\s*server_name\s+[^;]+' "$f" | sed -E 's/^\s*server_name\s+//' | tr ' ' '\n' | grep -v '^$')
    if [[ "$(basename "$f")" == *sahno* ]] || { [[ -n "$names" ]] && ! grep -qvi sahno <<<"$names"; }; then
      info "$f serves only Sahno: removing"
      removed_nginx+=("$f")
    else
      warn "$f mentions sahno alongside other sites; edit it by hand: $(grep -ni sahno "$f" | head -n3 | paste -sd' ')"
    fi
  done
  if ((${#removed_nginx[@]})) && $APPLY; then
    backup=/root/nginx-sahno-backup-$STAMP; mkdir -p "$backup"
    for f in "${removed_nginx[@]}"; do
      cp -a --parents "$f" "$backup"
      # Remove any sites-enabled symlink to it as well as the file.
      find /etc/nginx -xtype l -lname "*$(basename "$f")" -delete 2>/dev/null
      find /etc/nginx -type l -lname "$f" -delete 2>/dev/null
      rm -f "$f"
    done
    find /etc/nginx -xtype l -name '*sahno*' -delete 2>/dev/null
    if nginx -t >/dev/null 2>&1; then
      systemctl reload nginx 2>/dev/null || nginx -s reload
      info "  nginx -t passed and nginx reloaded; backup in $backup"
    else
      cp -a "$backup"/etc/nginx/. /etc/nginx/
      warn "nginx -t failed without the Sahno files, so they were put back (symlinks in sites-enabled may need re-creating); see $backup"
    fi
  fi
else
  info "nginx not installed"
fi

say "Let's Encrypt (certbot)"
if have certbot; then
  mapfile -t certs < <(certbot certificates 2>/dev/null | awk '/Certificate Name:/ {print $3}' | grep -i sahno)
  ((${#certs[@]})) || info "nothing found"
  for c in "${certs[@]}"; do run certbot delete --non-interactive --cert-name "$c"; done
else
  info "certbot not installed"
fi

# --- Firewall -----------------------------------------------------------------

say "Firewall (ufw, port $HTTPS_PORT)"
if have ufw && ufw status | grep -q '^Status: active'; then
  if ss -Hltnu "( sport = :$HTTPS_PORT )" 2>/dev/null | grep -q . && ! $APPLY; then
    info "port $HTTPS_PORT is in use now (expected: Sahno's Caddy); rechecked after Docker is cleared"
  fi
  if $APPLY && ss -Hltnu "( sport = :$HTTPS_PORT )" 2>/dev/null | grep -q .; then
    warn "something other than Sahno still listens on $HTTPS_PORT, so its ufw rules were kept"
  else
    rules=$(ufw status | grep -E "^$HTTPS_PORT(/tcp|/udp)?\s" || true)
    if [[ -n "$rules" ]]; then
      printf '%s\n' "$rules" | sed 's/^/     /'
      for proto in tcp udp; do
        ufw status | grep -qE "^$HTTPS_PORT/$proto\s" && run ufw --force delete allow "$HTTPS_PORT/$proto"
      done
      ufw status | grep -qE "^$HTTPS_PORT\s" && run ufw --force delete allow "$HTTPS_PORT"
    else
      info "no rules for $HTTPS_PORT"
    fi
  fi
else
  info "ufw not active"
fi
info "If the cloud provider's firewall opens $HTTPS_PORT for Sahno, close it there too."

# --- systemd and cron ---------------------------------------------------------

say "systemd and cron"
mapfile -t units < <(systemctl list-unit-files --no-legend 2>/dev/null | awk '{print $1}' | grep -i sahno)
for u in "${units[@]}"; do
  info "unit $u"
  run systemctl disable --now "$u"
  path=$(systemctl show -p FragmentPath --value "$u" 2>/dev/null)
  [[ -n "$path" && -f "$path" ]] && run rm -f "$path"
done
((${#units[@]})) && run systemctl daemon-reload
for f in /etc/cron.d/* /etc/crontab /var/spool/cron/crontabs/*; do
  [[ -f "$f" ]] && grep -qi sahno "$f" && warn "cron entry mentioning sahno in $f; remove it by hand"
done
((${#units[@]})) || info "no systemd units"

# --- The checkout -------------------------------------------------------------

say "Checkout $SAHNO_DIR"
if [[ -d "$SAHNO_DIR" ]]; then
  # Never rm -rf anything that is not recognisably the Sahno checkout.
  if [[ "$SAHNO_DIR" == / || ! -f "$SAHNO_DIR/deploy/test-server/compose.yaml" ]]; then
    warn "$SAHNO_DIR does not look like the Sahno checkout; not deleting it"
  else
    find "$SAHNO_DIR" -name '.env' -o -name '.env.*' -not -name '*.example' | while read -r secret; do
      [[ -f "$secret" ]] && run shred -u -z "$secret"
    done
    run rm -rf "$SAHNO_DIR"
  fi
else
  info "not found"
fi

# --- Anything left ------------------------------------------------------------

if $APPLY; then
  say "Leftovers (anything still mentioning sahno)"
  left=$(grep -rliI sahno /etc /srv /opt /root /home 2>/dev/null | grep -v 'sahno-backup-' | head -n 30)
  dleft=$(have docker && docker ps -a --format '{{.Names}}' | grep -i sahno; have docker && docker images | grep -i sahno)
  [[ -n "$left$dleft" ]] && printf '%s\n' "$left" "$dleft" | grep . | sed 's/^/     /' || info "none"
  info "Backups of edited proxy configs (*.sahno-backup-$STAMP, /root/nginx-sahno-backup-$STAMP) can be deleted once the proxy is confirmed fine."
  info "Shell history may still mention Sahno: history -c, and edit ~/.bash_history."
fi

if ((${#MANUAL[@]})); then
  say "Needs you"
  for m in "${MANUAL[@]}"; do info "- $m"; done
fi

say "Not on this server; do these yourself"
info "- Cloudflare: delete the A record for $SAHNO_HOST (or point it elsewhere), and revoke the DNS API token"
info "- Supabase: rotate the database password (the connection string was in .env)"
info "- GHCR: revoke the read:packages token used for docker login, if any"
info "- Resend / Expo access tokens from .env: rotate if they were set"
info "- EAS: change EXPO_PUBLIC_API_URL in the preview environment, or test builds keep calling this host"

$APPLY || printf '\nDry run only. Run again with --yes to apply.\n'
