import { ReadRequest } from "../api/ReadRequest";

export class Query {
  private _maxResults?: number;
  private _page: number = 1;
  private _fields: string[];
  private _tableName: string = "";
  private _filters: Filter[] = [];
  private _joins: Join[] = [];
  private _orderBy?: OrderBy[];
  private _distinct: boolean = false;
  private _denoramlize: boolean = false;
  private _except: Exclude[] = [];

  constructor(fields: string[]) {
    this._fields = fields;
    return this;
  }

  top(maxResults: number) {
    this._maxResults = maxResults;
    return this;
  }

  page(page: number) {
    this._page = page;
    return this;
  }

  from(tableName: string) {
    this._tableName = tableName;
    return this;
  }

  where(filter: Filter | string, operator?: operators, value?: any) {
    if (filter instanceof Filter) {
      this._filters.push(filter);
    } else {
      this._filters.push(exp(filter, operator, value));
    }
    return this;
  }

  and(filter: Filter | string, operator?: operators, value?: any) {
    if (filter instanceof Filter) {
      this._filters[this._filters.length - 1].and(filter);
    } else {
      this._filters[this._filters.length - 1].and(filter, operator, value);
    }
    return this;
  }

  or(filter: Filter | string, operator?: operators, value?: any) {
    if (filter instanceof Filter) {
      this._filters[this._filters.length - 1].or(filter);
    } else {
      this._filters[this._filters.length - 1].or(filter, operator, value);
    }
    return this;
  }

  join(
    from: string,
    to: string,
    alias: string,
    fields: string[] | null = null
  ) {
    this._joins.push(new Join(from, to, alias, fields, "inner"));
    return this;
  }

  leftJoin(
    from: string,
    to: string,
    alias: string,
    fields: string[] | null = null
  ) {
    this._joins.push(new Join(from, to, alias, fields, "left"));
    return this;
  }

  except(except: Exclude) {
    this._except.push(except);
    return this;
  }

  orderBy(field: string, order?: "asc" | "desc") {
    if (!this._orderBy) {
      this._orderBy = [];
    }
    this._orderBy.push({ field: field, order: order });
    return this;
  }

  distinct() {
    this._distinct = true;
    return this;
  }

  denormalize() {
    this._denoramlize = true;
    return this;
  }

  toReadRequest() {
    return {
      tableName: this._tableName,
      fields: this._fields,
      page: this._page ?? 1,
      maxResults: this._maxResults ?? null,
      orderBy: this._orderBy ?? [],
      filters: this._filters.length > 0 ? [this._filters[0].getJson()] : [],
      joins: this._joins,
      distinct: this._distinct,
      denormalize: this._denoramlize,
      except: this._except,
    } as ReadRequest;
  }
}

interface OrderBy {
  field: string;
  order?: "asc" | "desc";
}

interface Exclude {
  fromField: string;
  query: ReadRequest;
}

type joinType = "inner" | "left";

class Join {
  fields: string[];
  alias: string | null = null;
  fromTable: string;
  fromField: string;
  toTable: string;
  toField: string;
  type?: joinType;

  constructor(
    from: string,
    to: string,
    alias: string | null = null,
    fields: string[] | null = null,
    type?: joinType
  ) {
    let fromParts = from.split(".");
    let toParts = to.split(".");
    if (fromParts.length < 2 || toParts.length < 2) {
      throw new Error(
        "Join must include a table and field part ie: tableName.fieldName"
      );
    }
    this.fromTable = fromParts[0];
    this.fromField = fromParts[1];
    this.toTable = toParts[0];
    this.toField = toParts[1];
    this.alias = alias;
    this.fields = fields ?? [];
    this.type = type;
  }
}

class Filter {
  logicalOperator?: "AND" | "OR" = "AND";
  filters: Filter[] = [];
  conditions: Condition[] = [];

  lastCondition?: Condition;

  and(field: string | Filter, operator?: operators, value?: any) {
    if (field instanceof Filter) {
      this.filters.push(field);
    } else {
      if (this.logicalOperator == "OR") {
        let filter;
        // If the last condition added was added to the root filter, move it into it's own new filter
        // with the new condition
        let rootLastCondition = this.conditions[this.conditions.length - 1];
        if (this.lastCondition == rootLastCondition) {
          filter = new Filter();
          this.conditions.pop();
          filter.conditions.push(rootLastCondition);
        } else {
          filter = this.filters[this.filters.length - 1];
        }

        this.lastCondition = {
          field: field as string,
          operator: operator,
          value: value,
        } as Condition;
        filter.conditions.push(this.lastCondition);
        this.filters.push(filter);

        return this;
      }

      this.lastCondition = {
        field: field as string,
        operator: operator,
        value: value,
      } as Condition;

      this.conditions.push(this.lastCondition);
    }

    return this;
  }

  or(field: string | Filter, operator?: operators, value?: any) {
    if (field instanceof Filter) {
      this.filters.push(field);
    } else {
      // if this the first or?
      if (this.logicalOperator === "AND") {
        this.logicalOperator = "OR";
        let filter = new Filter();
        filter.conditions = this.conditions;
        this.filters.push(filter);
        this.conditions = [];
      }

      this.lastCondition = {
        field: field as string,
        operator: operator,
        value: value,
      } as Condition;

      this.conditions.push(this.lastCondition);
    }

    return this;
  }

  getJson(): any {
    let json = {
      logicalOperator: this.logicalOperator,
      filters: [...this.conditions, ...this.filters.map((x) => x.getJson())],
    };

    return json;
  }

  getStr(): string {
    let conditionsArray = [];
    for (let c of this.conditions) {
      conditionsArray.push(`${c.field} ${c.operator} ${c.value}`);
    }
    for (let f of this.filters) {
      conditionsArray.push(`(${f.getStr()})`);
    }
    return conditionsArray.join(` ${this.logicalOperator} `);
  }

  print() {
    console.log(this.getStr());
    return this;
  }
}

interface Condition {
  field?: string;
  operator?: string;
  value?: string;
}

type operators = "==" | "!=" | ">" | "<" | ">=" | "<=" | "Contains" | undefined;

export const exp = (field: string, operator: operators, value: any) => {
  let filter = new Filter();
  filter.logicalOperator = "AND";

  filter.conditions = [];
  filter.conditions.push({
    field: field,
    operator: operator,
    value: value,
  });

  return filter;
};

export default Query;

// let json = new Query(["*"])
//   .from("User")
//   .join("Option.OptionId", "User.OptionId", "opt")
//   .where(
//     exp("Name", "==", "Ben")
//       .and("Status", "==", "Active")
//       .and("Last Name", "==", "Labbe")
//       .and(
//         exp("Type", "==", "Customer")
//           .and("Type", "==", "xxx")
//           .or("Type", "==", "Partner")
//           .or("Type", "==", "Business")
//       )
//   )
//   .toJson();

// exp("Name", "==", "Ben").and("Status", "==", "Active").and("Last Name", "==", "Labbe").getJson()

// let json = exp("Name", "==", "Ben").and("Status", "==", "Active").and("Last Name", "==", "Labbe")
// .and(
//     exp("Type", "==", "Customer")
//     .and("Type", "==", "xxx")
//     .or("Type", "==", "Partner")
//     .or("Type", "==", "Business")
// ).print().getJson();
// console.log(json);

// exp("Name", "==", "Ben").and("Status", "==", "Active").and("Last Name", "==", "Labbe")
// .and(
//     exp("Type", "==", "Customer")
//     .and("Type", "==", "Partner")
//     .and("Type", "==", "Business")
//     .or("Exec", "==", "Corporate")
//     .and("Pet", "==", "Bird")
//     .and("Gardner", "==", "Richard")
//     .or("Last", "==", "test")
//     .or("once","==", "again")
//     .or("second", "==", "again")
//     .and("blahhh", "==", "none")).print();

// exp("Name", "==", "Ben").and("Status", "==", "Active").and("Last Name", "==", "Labbe")
// .and(
//     exp("Type", "==", "Customer").and("Type", "==", "Partner").and("Type", "==", "Business").or("Exec", "==", "Corporate").and("Pet", "==", "Bird")
//     ).print();

// if (type == customer && type == partner && type == business || exec == corporate && pet == bird)
// exp("name", "==", "Ben").or().or()

// new Query(["*"])
// .from("User")
// .where(exp("name", "==", "Ben").or().or())
// .and(exp("xyz", "!=", "xxx"))
// .and(exp("xxx", "==", "123"))
// .toJson()

// new Query(["*"])
// .from("User")
//     .join("User.UserId", "UserRole.UserId", "usrRole", ["UserRoleId"])
// .where("Name", "==", "Ben")
//     .or("usrRole.UserRoleId", "==", "xxx-xx-xx-xxxx")
//     .and("Status", "==", "Active")
//     .and("xxx", "==", "xxx")
//     .or("xx", "==", "zz")
//     // .andGroup("OR", [exp("Name", "==", "Jen"), exp("Status", "==", "Active")])
// .toJson();
