import { ReadOrderBy } from "./ReadRequest";

export type ReadResponse<T, U = any> = {
  pages: number;
  currentPage: number;
  totalResults: number;
  maxResults: number;
  tableName: string;
  orderBy?: ReadOrderBy[];
  results: T[];
  parameters: U;
};
