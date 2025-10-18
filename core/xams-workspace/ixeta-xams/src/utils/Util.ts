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

export function toLocalTimeStringWithAMPM(isoString: string) {
  const date = new Date(isoString);

  const months = [
    "Jan",
    "Feb",
    "Mar",
    "Apr",
    "May",
    "Jun",
    "Jul",
    "Aug",
    "Sep",
    "Oct",
    "Nov",
    "Dec",
  ];

  const day = date.getDate();
  const month = months[date.getMonth()];
  const year = date.getFullYear();

  let hours = date.getHours();
  const ampm = hours >= 12 ? "PM" : "AM";
  hours = hours % 12;
  hours = hours ? hours : 12; // 0 should be 12

  const minutes = date.getMinutes().toString().padStart(2, "0");
  const seconds = date.getSeconds().toString().padStart(2, "0");
  const milliseconds = date.getMilliseconds().toString().padStart(3, "0");

  return `${day} ${month} ${year} ${hours}:${minutes}:${seconds}.${milliseconds} ${ampm}`;
}

export function hasTimePart(dateFormat?: string): boolean {
  // If a time part is not included in the date format then remove it
  // Based on dayjs formats - https://day.js.org/docs/en/display/format
  if (dateFormat == null) {
    return false;
  }

  const timeParts = [
    "h",
    "m",
    "s",
    "A",
    "a",
    "H",
    "k",
    "K",
    "m",
    "s",
    "S",
    "Z",
    "X",
    "LT",
    "LTS",
    "LLL",
    "LLLL",
    "lll",
    "llll",
  ];

  if (timeParts.some((x) => dateFormat.includes(x))) {
    return true;
  }

  return false;
}
