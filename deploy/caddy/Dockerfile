# Caddy with the Cloudflare DNS module, for the test server
# (deploy/test-server). Ports 80 and 443 there belong to another project, so
# Caddy cannot prove the domain over HTTP; it proves it through Cloudflare's
# DNS API instead (a TXT record), which needs this module. Built on GitHub by
# .github/workflows/caddy-image.yml — the server only pulls it.

FROM caddy:2-builder AS builder
RUN xcaddy build --with github.com/caddy-dns/cloudflare

FROM caddy:2
COPY --from=builder /usr/bin/caddy /usr/bin/caddy
