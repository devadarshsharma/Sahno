import { PickerField } from '@/components/ui/picker-field';

export type DateFieldProps = {
  label: string;
  /** ISO calendar date, "YYYY-MM-DD", or null when not set. */
  value: string | null;
  onChange: (value: string | null) => void;
  placeholder?: string;
  helperText?: string;
  error?: string;
  /** Offers a "Clear" action. Only for dates that are allowed to be unknown. */
  clearable?: boolean;
};

/**
 * A tappable date field backed by the platform picker, behaving as each
 * platform's own apps do (PickerField, D-107). Typing dates by hand invites
 * the wrong month and the wrong format; a picker cannot produce either.
 *
 * The value is a plain calendar date rather than an instant: an engagement
 * happens on a day, and turning it into a timestamp would let a time zone move
 * it to the day before.
 */
export function DateField({ placeholder = 'Choose a date', ...props }: DateFieldProps) {
  return (
    <PickerField
      {...props}
      mode="date"
      placeholder={placeholder}
      toDate={parseIsoDate}
      fromDate={toIsoDate}
      format={formatIsoDate}
      suggested={() => new Date()}
    />
  );
}

/** Local calendar date, so a time zone cannot shift it to the day before. */
export function toIsoDate(date: Date): string {
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

export function parseIsoDate(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day);
}

export function formatIsoDate(value: string): string {
  return parseIsoDate(value).toLocaleDateString(undefined, {
    weekday: 'short',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
}
