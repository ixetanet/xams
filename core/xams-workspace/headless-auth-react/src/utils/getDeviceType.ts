export const getMobileDevice = () => {
  const userAgent =
    navigator.userAgent || navigator.vendor || (window as any).opera;

  // Android detection
  if (/android/i.test(userAgent)) {
    return "Android";
  }

  // iOS detection
  if (/iPad|iPhone|iPod/.test(userAgent) && !(window as any).MSStream) {
    return "iOS";
  }

  return "unknown";
};
