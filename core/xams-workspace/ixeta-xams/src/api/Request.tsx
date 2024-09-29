export interface Request {
  tableName?: string;
  fields?: any;
  parameters?: any;
  entities?: Request[] | any[];
}
