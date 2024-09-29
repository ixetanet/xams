import { create } from "zustand";

interface AuthStore {
  accessToken: string | undefined;
  setAccessToken: (accessToken: string | undefined) => void;
  userId?: string | undefined;
  setUserId: (userId: string | undefined) => void;
}

export const useAuthStore = create<AuthStore>((set) => ({
  accessToken: undefined,
  setAccessToken: (accessToken) => set({ accessToken }),
  userId: undefined,
  setUserId: (userId) => set({ userId }),
}));

export default useAuthStore;
