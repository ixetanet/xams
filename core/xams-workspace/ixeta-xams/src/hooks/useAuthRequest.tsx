import React, { useContext, useEffect, useMemo } from "react";
import { ApiResponse } from "../api/ApiResponse";
import { AppContext } from "../contexts/AppContext";
import {
  API_DATA_ACTION,
  API_DATA_CREATE,
  API_DATA_DELETE,
  API_DATA_FILE,
  API_DATA_METADATA,
  API_DATA_READ,
  API_DATA_UPDATE,
  API_DATA_PERMISSIONS,
  API_DATA_UPSERT,
  API_DATA_BULK,
} from "../apiurls";
import { ReadRequest } from "../api/ReadRequest";
import { MetadataResponse } from "../api/MetadataResponse";
import { ReadResponse } from "../api/ReadResponse";
import { TablesResponse } from "../api/TablesResponse";
import { useAuthStore } from "../stores/useAuthStore";
import { AuthContext } from "../contexts/AuthContext";
import { Request } from "../api/Request";
import { ActionRequest } from "../api/ActionRequest";
import { BulkRequest } from "../api/BulkRequest";

interface RequestParams {
  method: string;
  url: string;
  searchParams?: URLSearchParams;
  body?: ReadRequest | Request | ActionRequest | FormData | any;
  failureMessage?: string;
  hideFailureMessage?: boolean;
  fileName?: string;
  headers?: { [key: string]: string };
}

interface useAuthRequestProps {}

// If the user is not authenticated, the browswer will redirect to the home page
const useAuthRequest = (props?: useAuthRequestProps) => {
  // We want to be able to use useAuthRequest when not in a AuthContextProvider or AppContextProvider
  const appContext = useContext(AppContext);
  const authContext = useContext(AuthContext);
  const authStore = useAuthStore();

  const saveAsFile = (blob: Blob, filename: string) => {
    // Create an anchor element
    let a = document.createElement("a");

    // Use the blob as the href
    a.href = URL.createObjectURL(blob);

    // Set the download attribute with a filename
    a.download = filename;

    // Append the anchor to the body (required for Firefox)
    document.body.appendChild(a);

    // Trigger a click event on the anchor
    a.click();

    // Clean up: remove the anchor from the body and revoke the blob URL
    setTimeout(() => {
      document.body.removeChild(a);
      URL.revokeObjectURL(a.href);
    }, 0);
  };
  // const makeRequest = async (params: RequestParams): Promise<ApiResponse> => {
  const makeRequest = async <T,>(
    params: RequestParams
  ): Promise<ApiResponse<T>> => {
    try {
      let url = (authContext?.apiUrl ?? "") + params.url;
      if (params.searchParams !== undefined) {
        url += `?${params.searchParams.toString()}`;
      }

      // If this is being viewed from the mobile app, get the access token from the mobile app instead of next-auth
      let accessToken = authStore.accessToken; //session.data?.accessToken;
      const resp = await fetch(url, {
        method: params.method,
        headers: {
          Authorization: `Bearer ${accessToken}`,
          UserId: authStore.userId ?? "",
          ...(params.body !== undefined &&
            !(params.body instanceof FormData) && {
              // If body is FormData then don't set Content-Type to application/json
              "Content-Type": "application/json",
            }),
          ...params.headers,
          ...(authContext?.headers !== undefined && authContext?.headers),
        },
        ...(params.body !== undefined &&
          !(params.body instanceof FormData) && {
            body: JSON.stringify(params.body),
          }),
        ...(params.body !== undefined &&
          params.body instanceof FormData && { body: params.body }),
      });
      // If Unauthorized and we've attempted to silently refresh the token and the token is still not working, signout and direct to home page
      if (resp.status === 401) {
        if (authContext?.onUnauthorized !== undefined) {
          authContext.onUnauthorized();
        }
        return {
          succeeded: false,
          data: undefined,
          friendlyMessage: "",
          logMessage: "",
          response: resp,
        } as ApiResponse<T>;
      }
      if (resp.status === 400 && !params.hideFailureMessage) {
        if (params.failureMessage !== undefined) {
          appContext?.showError(params.failureMessage);
        } else {
          const apiResponse = (await resp.json()) as ApiResponse<any>;
          appContext?.showError(apiResponse.friendlyMessage);
          console.error(apiResponse.logMessage);
          return apiResponse;
        }
      }
      if (resp.ok === false) {
        const message = await resp.text();
        return {
          succeeded: false,
          data: undefined,
          friendlyMessage: message,
          logMessage: message,
          response: resp,
        } as ApiResponse<T>;
      }
      // If we received a json response
      const contentType = resp.headers.get("content-type");
      if (
        contentType &&
        contentType.indexOf("application/octet-stream") !== -1
      ) {
        saveAsFile(await resp.blob(), params.fileName ?? "file");
      }
      if (contentType && contentType.indexOf("application/json") !== -1) {
        const json = (await resp.json()) as ApiResponse<T>;
        if (json.succeeded === false) {
          if (
            params.hideFailureMessage === undefined ||
            params.hideFailureMessage === false
          ) {
            appContext?.showError(json.friendlyMessage);
          }
          console.error(json.logMessage);
        }
        return json;
      } else {
        // Request succeeded, but the response is not Json
        return {
          succeeded: true,
          data: resp.body as any,
          friendlyMessage: "",
          logMessage: "",
          response: resp,
        };
      }
    } catch (error) {
      console.error(error);
      // signOut({ callbackUrl: "/" });
      if (
        (params.hideFailureMessage === undefined ||
          params.hideFailureMessage === false) &&
        params.failureMessage !== undefined
      ) {
        appContext?.showError(JSON.stringify(error));
      }
      return {
        succeeded: false,
        data: undefined,
        friendlyMessage: "",
        logMessage: "",
        response: undefined,
      } as ApiResponse<T>;
    }
  };

  const apiHasAllPermissions = async (permissions: string[]) => {
    const resp = await makeRequest({
      url: API_DATA_PERMISSIONS,
      method: "POST",
      body: {
        method: "has_permissions",
        parameters: {
          permissionNames: permissions,
        },
      },
    });
    if (resp.succeeded === true) {
      const permissionsData = resp.data as string[];
      if (permissionsData.length === permissions.length) {
        return true;
      }
    }
    return false;
  };

  const apiHasAnyPermissions = async (permissions: string[]) => {
    const resp = await makeRequest({
      url: API_DATA_PERMISSIONS,
      method: "POST",
      body: {
        method: "has_permissions",
        parameters: {
          permissionNames: permissions,
        },
      },
    });
    if (resp.succeeded === true) {
      const permissionsData = resp.data as string[];
      if (permissionsData.length > 0) {
        return true;
      }
    }
    return false;
  };

  const apiMetadata = async (tableName: string) => {
    const resp = await makeRequest<MetadataResponse>({
      url: API_DATA_METADATA,
      method: "POST",
      body: {
        method: "table_metadata",
        parameters: {
          tableName: tableName,
        },
      },
    });
    return resp?.data as MetadataResponse;
  };

  const apiTables = async (tag?: string) => {
    const resp = await makeRequest<TablesResponse[]>({
      url: API_DATA_METADATA,
      method: "POST",
      body: {
        method: "table_list",
        parameters: {
          tag: tag,
        },
      },
    });
    return resp;
  };

  const apiCreate = async <T,>(
    tableName: string,
    fields: T,
    parameters: any = null
  ) => {
    const resp = await makeRequest<T>({
      method: "POST",
      url: API_DATA_CREATE,
      body: {
        tableName: tableName,
        fields: fields,
        parameters: parameters,
      },
    });
    return resp;
  };

  const apiUpdate = async <T,>(
    tableName: string,
    fields: T,
    parameters?: any
  ) => {
    const resp = await makeRequest<T>({
      method: "PATCH",
      url: API_DATA_UPDATE,
      body: {
        tableName: tableName,
        fields: fields,
        parameters: parameters,
      },
    });
    return resp;
  };

  const apiDelete = async <T,>(
    tableName: string,
    id: string,
    parameters?: any
  ) => {
    const resp = await makeRequest<T>({
      method: "DELETE",
      url: API_DATA_DELETE,
      body: {
        tableName: tableName,
        fields: {
          [`${tableName}Id`]: id,
        },
        parameters: parameters,
      },
    });
    return resp;
  };

  const apiUpsert = async <T,>(
    tableName: string,
    fields: T,
    parameters?: any
  ) => {
    const resp = await makeRequest<T>({
      method: "POST",
      url: API_DATA_UPSERT,
      body: {
        tableName: tableName,
        fields: fields,
        parameters: parameters,
      },
    });
    return resp;
  };

  const apiBulkCreate = async <T,>(entities: T[], parameters: any = null) => {
    const resp = await makeRequest<T>({
      method: "POST",
      url: API_DATA_CREATE,
      body: {
        entities: entities,
        parameters: parameters,
      },
    });
    return resp;
  };

  const apiBulkUpdate = async <T,>(entities: T[], parameters: any = null) => {
    const resp = await makeRequest<T>({
      method: "PATCH",
      url: API_DATA_UPDATE,
      body: {
        entities: entities,
        parameters: parameters,
      },
    });
    return resp;
  };

  const apiBulkDelete = async <T,>(entities: T[], parameters: any = null) => {
    const resp = await makeRequest<T>({
      method: "DELETE",
      url: API_DATA_DELETE,
      body: {
        entities: entities,
        parameters: parameters,
      },
    });
    return resp;
  };

  const apiBulkUpsert = async <T,>(entities: T[], parameters: any = null) => {
    const resp = await makeRequest<T>({
      method: "POST",
      url: API_DATA_UPSERT,
      body: {
        entities: entities,
        parameters: parameters,
      },
    });
    return resp;
  };

  const apiBulk = async <T,>(request: BulkRequest, parameters: any = null) => {
    const resp = await makeRequest<T>({
      url: API_DATA_BULK,
      method: "POST",
      body: request,
    });
    return resp;
  };

  const apiRead = async <T,>(body: ReadRequest) => {
    const resp = (await makeRequest<T>({
      method: "POST",
      url: API_DATA_READ,
      body: body,
    })) as ApiResponse<ReadResponse<T>>;
    return resp;
  };

  const apiAction = async <T,>(
    actionName: string,
    parameters?: any,
    fileName?: string
  ) => {
    const resp = await makeRequest<T>({
      method: "POST",
      url: API_DATA_ACTION,
      body: {
        name: actionName,
        parameters: parameters,
      },
      fileName: fileName,
    });
    return resp;
  };

  const apiFile = async <T,>(formData: FormData) => {
    const resp = await makeRequest<T>({
      method: "POST",
      url: API_DATA_FILE,
      body: formData,
    });
    return resp;
  };

  return useMemo(
    () => ({
      execute: makeRequest,
      hasAllPermissions: apiHasAllPermissions,
      hasAnyPermissions: apiHasAnyPermissions,
      tables: apiTables,
      metadata: apiMetadata,
      create: apiCreate,
      read: apiRead,
      update: apiUpdate,
      delete: apiDelete,
      upsert: apiUpsert,
      bulkCreate: apiBulkCreate,
      bulkUpdate: apiBulkUpdate,
      bulkDelete: apiBulkDelete,
      bulkUpsert: apiBulkUpsert,
      bulk: apiBulk,
      action: apiAction,
      file: apiFile,
    }),
    [
      authContext?.apiUrl,
      authContext?.headers,
      authContext?.onUnauthorized,
      authStore.accessToken,
      authStore.userId,
    ]
  );
};
export type useAuthRequestType = ReturnType<typeof useAuthRequest>;

export default useAuthRequest;
