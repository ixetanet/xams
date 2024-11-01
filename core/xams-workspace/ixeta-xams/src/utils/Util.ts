export function isNotNull<T>(value: T | null): value is T {
  return value !== null;
}

// returns the offset of the local timezone from UTC in hours
export function getLocalTimeOffsetFromUTC(): number {
  const date = new Date();
  const timezoneOffsetInMinutes = date.getTimezoneOffset();
  const timezoneOffsetInHours = timezoneOffsetInMinutes / 60;
  return -timezoneOffsetInHours;
}
