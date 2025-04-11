export function getQueryParam(name: string, url = window.location.href) {
  name = name.replace(/[\[\]]/g, "\\$&");
  const regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)");
  const results = regex.exec(url);
  if (!results) return null;
  if (!results[2]) return "";
  return decodeURIComponent(results[2].replace(/\+/g, " "));
}

export function addUserIdUrlParam(currentUrl: string, destinationUrl: string) {
  const userId = getQueryParam("userid", currentUrl);
  if (userId != null) {
    let userIdPart = `userid=${userId}`;
    if (destinationUrl.includes("?")) {
      userIdPart = `&${userIdPart}`;
    } else {
      userIdPart = `?${userIdPart}`;
    }
    return destinationUrl + userIdPart;
  } else {
    return destinationUrl;
  }
}
