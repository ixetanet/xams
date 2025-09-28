export interface ApiResponse<T> {
  succeeded: boolean;
  data: T;
  friendlyMessage: string;
  logMessage: string;
  response: Response | undefined;
}
