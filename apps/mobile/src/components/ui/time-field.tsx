import { PickerField } from '@/components/ui/picker-field';

export type TimeFieldProps = {
  label: string;
  /** "19:30:00", or null when not set. */
  value: string | null;
  onChange: (value: string | null) => void;
  placeholder?: string;
  helperText?: string;
  clearable?: boolean;
};

/**
 * A tappable time field backed by the platform picker, behaving as each
 * platform's own apps do (PickerField, D-107). Times are stored as a wall
 * clock — the hour written on the booking sheet — rather than an instant,
 * because a call time is "be there at five", not a moment on a timeline that
 * shifts with a device's zone.
 */
export function TimeField({ placeholder = 'Not set', clearable = true, ...props }: TimeFieldProps) {
  return (
    <PickerField
      {...props}
      mode="time"
      placeholder={placeholder}
      clearable={clearable}
      toDate={parseTime}
      fromDate={toTimeString}
      format={formatTime}
      suggested={defaultTime}
    />
  );
}

function defaultTime(): Date {
  const date = new Date();
  date.setHours(19, 0, 0, 0);
  return date;
}

export function toTimeString(date: Date): string {
  const hours = `${date.getHours()}`.padStart(2, '0');
  const minutes = `${date.getMinutes()}`.padStart(2, '0');
  return `${hours}:${minutes}:00`;
}

export function parseTime(value: string): Date {
  const [hours, minutes] = value.split(':').map(Number);
  const date = new Date();
  date.setHours(hours, minutes, 0, 0);
  return date;
}

export function formatTime(value: string): string {
  return parseTime(value).toLocaleTimeString(undefined, {
    hour: 'numeric',
    minute: '2-digit',
  });
}
