import { Request } from "./Request";

export interface BulkRequest {
  creates?: Request[];
  updates?: Request[];
  deletes?: Request[];
  upserts?: Request[];
}
