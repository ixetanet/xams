import { UIInfo } from "./UIInfo";

export interface AuditField {
  AuditFieldId: string;
  Name: string;
  AuditId: string;
  Audit: string;
  IsCreate: boolean;
  IsRead: boolean;
  IsUpdate: boolean;
  IsDelete: boolean;
  _ui_info_: UIInfo;
}
