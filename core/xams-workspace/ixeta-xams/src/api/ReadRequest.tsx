export interface ReadRequest {
  tableName: string;
  fields?: string[];
  id?: string;
  orderBy?: ReadOrderBy[];
  maxResults?: number;
  page?: number;
  filters?: ReadFilter[];
  joins?: ReadJoin[];
  except?: ReadExcept[];
  distinct?: boolean;
  denormalize?: boolean;
  parameters?: any;
}

export interface ReadJoin {
  fields: string[];
  alias?: string;
  fromTable: string;
  fromField: string;
  toTable: string;
  toField: string;
  filters?: ReadFilter[];
}

export interface ReadFilter {
  logicalOperator?: "AND" | "OR";
  filters?: ReadFilter[];
  field?: string;
  value?: string | null;
  operator?: string;
}

export interface ReadExcept {
  fromField: string; // The field in the current table
  query: ReadRequest;
}

export interface ReadOrderBy {
  field: string;
  order?: string;
}
