import { ReadOrderBy } from "./ReadRequest";

export interface ReadResponse<T> {
  pages: number;
  currentPage: number;
  totalResults: number;
  maxResults: number;
  tableName: string;
  orderBy?: ReadOrderBy[];
  results: T[];
}
