import { API_DATA_READ } from "../apiurls";
import { useAuthRequestType } from "../hooks/useAuthRequest";
import { create } from "zustand";

export interface LookupStoreInfo {
  fieldName: string;
  lookupTable: string;
  lookupTableNameField: string;
  value: string;
  label: string;
}

interface useLookupStoreDefaultsState {
  lookups: LookupStoreInfo[];
  getLookupLabel: (
    authRequest: useAuthRequestType,
    fieldName: string,
    lookupTable: string,
    lookupTableNameField: string,
    value: string
  ) => Promise<LookupStoreInfo | undefined>;
  getAnyLookup: (
    authRequest: useAuthRequestType,
    fieldName: string,
    lookupTable: string,
    lookupTableNameField: string
  ) => Promise<LookupStoreInfo | undefined>;
}

const useLookupStore = create<useLookupStoreDefaultsState>()((set, get) => ({
  lookups: [] as LookupStoreInfo[],
  getLookupLabel: async (
    authRequest: useAuthRequestType,
    fieldName: string,
    lookupTable: string,
    lookupTableNameField: string,
    value: string
  ) => {
    const lookup = get().lookups.find(
      (x) =>
        x.fieldName === fieldName &&
        x.lookupTable === lookupTable &&
        x.lookupTableNameField == lookupTableNameField &&
        x.value === value
    );
    if (lookup !== undefined) {
      return lookup;
    }

    const resp = await authRequest.execute<any>({
      url: API_DATA_READ,
      method: "POST",
      body: {
        fields: ["*"],
        tableName: lookupTable,
        maxResults: 1,
        page: 1,
        id: value,
      },
    });
    // If the user is missing read permissions
    if (resp?.succeeded === true && resp.data === null) {
      return undefined;
    }

    if (
      resp?.succeeded === true &&
      resp.data.results !== undefined &&
      resp.data.results.length > 0
    ) {
      const label = resp.data.results[0][lookupTableNameField];
      const result = {
        fieldName,
        lookupTable,
        lookupTableNameField,
        value,
        label,
      };
      set({
        lookups: [...get().lookups, result],
      });
      return result;
    }

    return undefined;
  },
  getAnyLookup: async (
    authRequest: useAuthRequestType,
    fieldName: string,
    lookupTable: string,
    lookupTableNameField: string
  ) => {
    const lookup = get().lookups.find(
      (x) =>
        x.fieldName === fieldName &&
        x.lookupTable === lookupTable &&
        x.lookupTableNameField == lookupTableNameField
    );
    if (lookup !== undefined) {
      return lookup;
    }

    const resp = await authRequest.execute<any>({
      url: API_DATA_READ,
      method: "POST",
      body: {
        fields: ["*"],
        tableName: lookupTable,
        maxResults: 1,
        page: 1,
      },
    });

    // If the user is missing read permissions
    if (resp?.succeeded === true && resp.data === null) {
      return undefined;
    }

    if (
      resp?.succeeded === true &&
      resp.data.results !== undefined &&
      resp.data.results.length > 0
    ) {
      const result = {
        fieldName,
        lookupTable,
        lookupTableNameField,
        value: resp.data.results[0][`${lookupTable}Id`],
        label: resp.data.results[0][lookupTableNameField],
      };
      set({
        lookups: [...get().lookups, result],
      });
      return result;
    }

    return undefined;
  },
}));

export default useLookupStore;
