import { useAuth } from "@ixeta/headless-auth-react";
import {
  Button,
  TextInput,
  Checkbox,
  Loader,
  Paper,
  Title,
  Text,
  Divider,
  Stack,
  Alert,
} from "@mantine/core";
import { useRouter } from "next/router";
import { sendEmailVerification } from "firebase/auth";
import React, { useState } from "react";
import { firebaseAuth } from "@/pages/_app";

interface LoginComponentProps {
  providers: string[];
}

const LoginComponent = (props: LoginComponentProps) => {
  const router = useRouter();
  const auth = useAuth();

  const getProviderDisplayName = (provider: string): string => {
    const displayNames: Record<string, string> = {
      google: "Google",
      facebook: "Facebook",
      twitter: "Twitter",
      github: "GitHub",
      apple: "Apple",
      microsoft: "Microsoft",
      yahoo: "Yahoo"
    };
    return displayNames[provider] || provider;
  };

  const getProviderIcon = (provider: string): React.ReactNode => {
    switch (provider) {
      case "google":
        return (
          <svg width="20" height="20" viewBox="0 0 24 24">
            <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
            <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
            <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
            <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
          </svg>
        );
      case "microsoft":
        return (
          <svg width="20" height="20" viewBox="0 0 24 24">
            <path fill="#f25022" d="M1 1h10v10H1z"/>
            <path fill="#00a4ef" d="M13 1h10v10H13z"/>
            <path fill="#7fba00" d="M1 13h10v10H1z"/>
            <path fill="#ffb900" d="M13 13h10v10H13z"/>
          </svg>
        );
      case "facebook":
        return (
          <svg width="20" height="20" viewBox="0 0 24 24">
            <path fill="#1877f2" d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/>
          </svg>
        );
      case "github":
        return (
          <svg width="20" height="20" viewBox="0 0 24 24">
            <path fill="#333333" d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
          </svg>
        );
      case "apple":
        return (
          <svg width="20" height="20" viewBox="0 0 814 1000">
            <path fill="#000000" d="M788.1 340.9c-5.8 4.5-108.2 62.2-108.2 190.5 0 148.4 130.3 200.9 134.2 202.2-.6 3.2-20.7 71.9-68.7 141.9-42.8 61.6-87.5 123.1-155.5 123.1s-85.5-39.5-164-39.5c-76.5 0-103.7 40.8-165.9 40.8s-105.6-57-155.5-127C46.7 790.7 0 663 0 541.8c0-194.4 126.4-297.5 250.8-297.5 66.1 0 121.2 43.4 162.7 43.4 39.5 0 101.1-46 176.3-46 28.5 0 130.9 2.6 198.3 99.2zm-234-181.5c31.1-36.9 53.1-88.1 53.1-139.3 0-7.1-.6-14.3-1.9-20.1-50.6 1.9-110.8 33.7-147.1 75.8-28.5 32.4-55.1 83.6-55.1 135.5 0 7.8 1.3 15.6 1.9 18.1 3.2.6 8.4 1.3 13.6 1.3 45.4 0 102.5-30.4 135.5-71.3z"/>
          </svg>
        );
      case "twitter":
        return (
          <svg width="20" height="20" viewBox="0 0 24 24">
            <path fill="#1DA1F2" d="M23.953 4.57a10 10 0 01-2.825.775 4.958 4.958 0 002.163-2.723c-.951.555-2.005.959-3.127 1.184a4.92 4.92 0 00-8.384 4.482C7.69 8.095 4.067 6.13 1.64 3.162a4.822 4.822 0 00-.666 2.475c0 1.71.87 3.213 2.188 4.096a4.904 4.904 0 01-2.228-.616v.06a4.923 4.923 0 003.946 4.827 4.996 4.996 0 01-2.212.085 4.936 4.936 0 004.604 3.417 9.867 9.867 0 01-6.102 2.105c-.39 0-.779-.023-1.17-.067a13.995 13.995 0 007.557 2.209c9.053 0 13.998-7.496 13.998-13.985 0-.21 0-.42-.015-.63A9.935 9.935 0 0024 4.59z"/>
          </svg>
        );
      case "yahoo":
        return (
          <svg width="20" height="20" viewBox="0 0 24 24">
            <path fill="#410093" d="M12 0C5.383 0 0 5.383 0 12s5.383 12 12 12 12-5.383 12-12S18.617 0 12 0zm6.281 19.032H15.4l-2.044-8.863-2.044 8.863H8.431l3.588-14.024h1.875l3.387 14.024z"/>
          </svg>
        );
      default:
        return null;
    }
  };

  const createProviderLoadingStates = () => {
    const providerStates: Record<string, boolean> = {};
    props.providers.forEach(provider => {
      providerStates[`${provider}SignIn`] = false;
    });
    return providerStates;
  };

  const [loadingStates, setLoadingStates] = useState<Record<string, boolean>>({
    signIn: false,
    signUp: false,
    mfaTotp: false,
    mfaSms: false,
    resendEmail: false,
    ...createProviderLoadingStates(),
  });

  const isAnyProviderLoading = () => {
    return props.providers.some(provider => loadingStates[`${provider}SignIn`] || false);
  };

  if (!auth.isReady) {
    return (
      <div className="min-h-screen flex justify-center items-center">
        <Loader size="md" />
      </div>
    );
  }

  // User is attempting to login and MFA is required
  if (!auth.isLoggedIn && auth.isMfaRequired) {
    if (auth.view !== "mfa_totp" && auth.view !== "mfa_sms") {
      return (
        <div className="min-h-screen flex justify-center items-center p-4">
          <Paper
            shadow="sm"
            radius="lg"
            p="xl"
            withBorder
            className="w-full max-w-sm"
          >
            <Stack gap="md">
              <Title order={3} ta="center">
                Choose verification method
              </Title>
              {/* Get the user enrolled mfa factors */}
              {auth.mfaFactors.map((factor) => {
                if (factor === "totp") {
                  return (
                    <Button
                      key={factor}
                      onClick={() => auth.setView("mfa_totp")}
                      variant="default"
                      size="md"
                      fullWidth
                      disabled={loadingStates.mfaTotp || loadingStates.mfaSms}
                    >
                      Authenticator App
                    </Button>
                  );
                }
                if (factor === "sms") {
                  return (
                    <Button
                      key={factor}
                      onClick={() => {
                        auth.setView("mfa_sms");
                        auth.mfaSmsSend();
                      }}
                      variant="default"
                      size="md"
                      fullWidth
                      disabled={loadingStates.mfaTotp || loadingStates.mfaSms}
                    >
                      SMS
                    </Button>
                  );
                }
                return null;
              })}
              <Button
                onClick={() => auth.signOut()}
                variant="subtle"
                size="sm"
                mt="xs"
                fullWidth
                disabled={loadingStates.mfaTotp || loadingStates.mfaSms}
              >
                Logout
              </Button>
            </Stack>
          </Paper>
        </div>
      );
    }
    if (auth.view === "mfa_totp") {
      return (
        <div className="min-h-screen flex justify-center items-center p-4">
          <Paper
            shadow="sm"
            radius="lg"
            p="xl"
            withBorder
            className="w-full max-w-sm"
          >
            <form
              onSubmit={async (e) => {
                e.preventDefault();
                setLoadingStates((prev) => ({ ...prev, mfaTotp: true }));
                try {
                  await auth.mfaTotpVerify();
                } finally {
                  setLoadingStates((prev) => ({ ...prev, mfaTotp: false }));
                }
              }}
            >
              <Stack gap="lg">
                <div>
                  <Title order={3} ta="center" mb="xs">
                    Enter verification code
                  </Title>
                  <Text size="sm" ta="center" c="dimmed">
                    Enter the code from your authenticator app
                  </Text>
                </div>
                {auth.error && (
                  <Alert color="red" variant="light">
                    {auth.error}
                  </Alert>
                )}
                <TextInput
                  label="Verification code"
                  placeholder="000000"
                  value={auth.mfaCode}
                  onChange={(e) => auth.setMfaCode(e.currentTarget.value)}
                  size="md"
                  disabled={loadingStates.mfaTotp}
                />
                <Button
                  type="submit"
                  size="md"
                  fullWidth
                  loading={loadingStates.mfaTotp}
                >
                  Verify
                </Button>
              </Stack>
            </form>
          </Paper>
        </div>
      );
    }
    if (auth.view === "mfa_sms") {
      return (
        <div className="min-h-screen flex justify-center items-center p-4">
          <Paper
            shadow="sm"
            radius="lg"
            p="xl"
            withBorder
            className="w-full max-w-sm"
          >
            <form
              onSubmit={async (e) => {
                e.preventDefault();
                setLoadingStates((prev) => ({ ...prev, mfaSms: true }));
                try {
                  await auth.mfaSmsVerify();
                } finally {
                  setLoadingStates((prev) => ({ ...prev, mfaSms: false }));
                }
              }}
            >
              <Stack gap="lg">
                <div>
                  <Title order={3} ta="center" mb="xs">
                    Enter SMS code
                  </Title>
                  <Text size="sm" ta="center" c="dimmed">
                    Enter the code sent to your phone
                  </Text>
                </div>
                {auth.error && (
                  <Alert color="red" variant="light">
                    {auth.error}
                  </Alert>
                )}
                <TextInput
                  label="SMS code"
                  placeholder="000000"
                  value={auth.mfaCode}
                  onChange={(e) => auth.setMfaCode(e.currentTarget.value)}
                  size="md"
                  disabled={loadingStates.mfaSms}
                />
                <Button
                  type="submit"
                  size="md"
                  fullWidth
                  loading={loadingStates.mfaSms}
                >
                  Verify
                </Button>
              </Stack>
            </form>
          </Paper>
        </div>
      );
    }
  }

  // The user is not logged in or a re-login is required for MFA
  if (!auth.isLoggedIn || auth.isReLoginRequired) {
    if (auth.view === "login") {
      return (
        <div className="min-h-screen flex justify-center items-center p-4">
          <Paper
            shadow="sm"
            radius="lg"
            p="xl"
            withBorder
            className="w-full max-w-sm"
          >
            <Stack gap="lg">
              <div>
                <Title order={2} ta="center" mb="xs">
                  Sign in
                </Title>
                <Text size="sm" ta="center" c="dimmed">
                  Please sign in to continue.
                </Text>
              </div>

              {auth.error && (
                <Alert color="red" variant="light">
                  {auth.error}
                </Alert>
              )}

              <form
                onSubmit={async (e) => {
                  e.preventDefault();
                  setLoadingStates((prev) => ({ ...prev, signIn: true }));
                  try {
                    await auth.signIn();
                  } finally {
                    setLoadingStates((prev) => ({ ...prev, signIn: false }));
                  }
                }}
              >
                <Stack gap="md">
                  <TextInput
                    label="Email address"
                    placeholder="Enter your email"
                    value={auth.emailAddress}
                    onChange={(e) =>
                      auth.setEmailAddress(e.currentTarget.value)
                    }
                    size="md"
                    disabled={
                      loadingStates.signIn ||
                      isAnyProviderLoading()
                    }
                  />
                  <TextInput
                    label="Password"
                    placeholder="Enter your password"
                    type="password"
                    value={auth.password}
                    onChange={(e) => auth.setPassword(e.currentTarget.value)}
                    size="md"
                    disabled={
                      loadingStates.signIn ||
                      isAnyProviderLoading()
                    }
                  />
                  <Checkbox
                    label="Remember me"
                    checked={auth.remember}
                    onChange={(e) => {
                      auth.setRemember(e.currentTarget.checked);
                    }}
                    disabled={
                      loadingStates.signIn ||
                      isAnyProviderLoading()
                    }
                  />
                </Stack>

                <Button
                  type="submit"
                  size="md"
                  mt="md"
                  fullWidth
                  loading={loadingStates.signIn}
                >
                  Sign in
                </Button>
              </form>

              {props.providers.length > 0 && (
                <>
                  <Divider label="Or continue with" labelPosition="center" />

                  <Stack gap="sm">
                    {props.providers.map((provider) => (
                      <Button
                        key={provider}
                        onClick={async () => {
                          const loadingKey = `${provider}SignIn`;
                          setLoadingStates((prev) => ({
                            ...prev,
                            [loadingKey]: true,
                          }));
                          try {
                            await auth.signInProvider(provider);
                          } finally {
                            setLoadingStates((prev) => ({
                              ...prev,
                              [loadingKey]: false,
                            }));
                          }
                        }}
                        variant="default"
                        size="md"
                        fullWidth
                        disabled={
                          loadingStates.signIn ||
                          isAnyProviderLoading()
                        }
                        loading={loadingStates[`${provider}SignIn`] || false}
                        leftSection={getProviderIcon(provider)}
                      >
                        Continue with {getProviderDisplayName(provider)}
                      </Button>
                    ))}
                  </Stack>
                </>
              )}

              <Text size="sm" ta="center">
                Don&apos;t have an account?{" "}
                <Text
                  component="a"
                  c="blue"
                  style={{ cursor: "pointer" }}
                  onClick={() => auth.setView("register")}
                >
                  Sign up
                </Text>
              </Text>
            </Stack>
          </Paper>
        </div>
      );
    }
    if (auth.view === "register") {
      return (
        <div className="min-h-screen flex justify-center items-center p-4">
          <Paper
            shadow="sm"
            radius="lg"
            p="xl"
            withBorder
            className="w-full max-w-sm"
          >
            <Stack gap="lg">
              <div>
                <Title order={2} ta="center" mb="xs">
                  Create account
                </Title>
                <Text size="sm" ta="center" c="dimmed">
                  Get started by creating your account.
                </Text>
              </div>

              {auth.error && (
                <Alert color="red" variant="light">
                  {auth.error}
                </Alert>
              )}

              <form
                onSubmit={async (e) => {
                  e.preventDefault();
                  setLoadingStates((prev) => ({ ...prev, signUp: true }));
                  try {
                    await auth.signUp();
                  } finally {
                    setLoadingStates((prev) => ({ ...prev, signUp: false }));
                  }
                }}
              >
                <Stack gap="md">
                  <TextInput
                    label="Email address"
                    placeholder="Enter your email"
                    value={auth.emailAddress}
                    onChange={(e) =>
                      auth.setEmailAddress(e.currentTarget.value)
                    }
                    size="md"
                    disabled={loadingStates.signUp}
                  />
                  <TextInput
                    label="Password"
                    placeholder="Create a password"
                    type="password"
                    value={auth.password}
                    onChange={(e) => auth.setPassword(e.currentTarget.value)}
                    size="md"
                    disabled={loadingStates.signUp}
                  />
                </Stack>

                <Button
                  type="submit"
                  size="md"
                  mt="md"
                  fullWidth
                  loading={loadingStates.signUp}
                >
                  Create account
                </Button>
              </form>

              <Text size="sm" ta="center">
                Already have an account?{" "}
                <Text
                  component="a"
                  c="blue"
                  style={{ cursor: "pointer" }}
                  onClick={() => auth.setView("login")}
                >
                  Sign in
                </Text>
              </Text>
            </Stack>
          </Paper>
        </div>
      );
    }
  }

  if (!auth.isEmailVerified) {
    return (
      <div className="min-h-screen flex justify-center items-center p-4">
        <Paper
          shadow="sm"
          radius="lg"
          p="xl"
          withBorder
          className="w-full max-w-sm"
        >
          <Stack gap="lg">
            <div>
              <Title order={3} ta="center" mb="xs">
                Verify your email
              </Title>
              <Text size="sm" ta="center" c="dimmed">
                We&apos;ve sent a verification link to your email address.
                Please check your inbox.
              </Text>
            </div>

            {auth.error && (
              <Alert color="red" variant="light">
                {auth.error}
              </Alert>
            )}

            <form
              onSubmit={async (e) => {
                e.preventDefault();
                setLoadingStates((prev) => ({ ...prev, resendEmail: true }));
                try {
                  if (firebaseAuth == null) {
                    return;
                  }
                  if (firebaseAuth.currentUser === null) {
                    return;
                  }
                  await sendEmailVerification(firebaseAuth.currentUser);
                } finally {
                  setLoadingStates((prev) => ({ ...prev, resendEmail: false }));
                }
              }}
            >
              <Button
                type="submit"
                size="md"
                fullWidth
                loading={loadingStates.resendEmail}
              >
                Resend verification email
              </Button>
            </form>
          </Stack>
        </Paper>
      </div>
    );
  }

  // If the user isn't enrolled in at least 1 mfa
  // if (!(auth.mfaTotpEnrolled || auth.mfaSmsEnrolled)) {
  //   if (
  //     auth.view !== "setup_mfa_totp" &&
  //     auth.view !== "setup_mfa_sms" &&
  //     auth.view !== "setup_mfa_sms_enroll"
  //   ) {
  //     return (
  //       <div className="w-full h-full flex justify-center items-center">
  //         <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
  //           Please setup mfa
  //           <Button
  //             onClick={async () => {
  //               auth.setView("setup_mfa_totp");
  //               auth.mfaTotpCreate();
  //             }}
  //           >
  //             Authenticator App
  //           </Button>
  //           <Button onClick={() => auth.setView("setup_mfa_sms")}>SMS</Button>
  //           <Button onClick={() => auth.signOut()}>Logout</Button>
  //         </div>
  //       </div>
  //     );
  //   }
  //   if (auth.view === "setup_mfa_totp") {
  //     return (
  //       <div className="w-full h-full flex justify-center items-center">
  //         <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
  //           <div className=" text-red-800">{auth.error}</div>
  //           Use an Authenticator App
  //           {auth.isProcessing && (
  //             <div className="w-full h-full flex justify-center items-center">
  //               <Loader />
  //             </div>
  //           )}
  //           {auth.mfaTotpQrCode && (
  //             <QRCode
  //               size={256}
  //               style={{ height: "auto", maxWidth: "100%", width: "100%" }}
  //               value={auth.mfaTotpQrCode}
  //               viewBox={`0 0 256 256`}
  //             />
  //           )}
  //           <TextInput
  //             label="MFA Code"
  //             value={auth.mfaCode}
  //             onChange={(e) => auth.setMfaCode(e.currentTarget.value)}
  //           ></TextInput>
  //           <Button onClick={() => auth.mfaTotpEnroll()}>Enroll</Button>
  //           <Button onClick={() => auth.signOut()}>Logout</Button>
  //         </div>
  //       </div>
  //     );
  //   }
  //   if (auth.view === "setup_mfa_sms") {
  //     return (
  //       <div className="w-full h-full flex justify-center items-center">
  //         <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
  //           <div className=" text-red-800">{auth.error}</div>
  //           Setup SMS MFA
  //           <TextInput
  //             label="Phone Number"
  //             value={auth.phoneNumber}
  //             onChange={(e) => auth.setPhoneNumber(e.currentTarget.value)}
  //           ></TextInput>
  //           <Button
  //             onClick={async () => {
  //               if (await auth.mfaSmsCreate()) {
  //                 auth.setView("setup_mfa_sms_enroll");
  //               }
  //             }}
  //           >
  //             Send Code
  //           </Button>
  //           <Button onClick={() => auth.signOut()}>Logout</Button>
  //         </div>
  //       </div>
  //     );
  //   }
  //   if (auth.view === "setup_mfa_sms_enroll") {
  //     return (
  //       <div className="w-full h-full flex justify-center items-center">
  //         <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
  //           <div className=" text-red-800">{auth.error}</div>
  //           Setup SMS MFA
  //           <TextInput
  //             label="Enter Code"
  //             value={auth.mfaCode}
  //             onChange={(e) => auth.setMfaCode(e.currentTarget.value)}
  //           ></TextInput>
  //           <Button onClick={() => auth.mfaSmsEnroll()}>Enroll</Button>
  //           <Button onClick={() => auth.mfaSmsCreate()}>Resend Code</Button>
  //           <Button onClick={() => auth.signOut()}>Logout</Button>
  //         </div>
  //       </div>
  //     );
  //   }
  // }

  // Login success
  router.push("/");
  // Route to the apps dashboard\internal landing page
  return (
    <></>
    // <div className="w-full h-full flex justify-center items-center flex-col gap-2">
    //   {auth.mfaTotpEnrolled && (
    //     <Button onClick={() => auth.mfaTotpUnenroll()}>Unenroll TOTP</Button>
    //   )}
    //   {auth.mfaSmsEnrolled && (
    //     <Button onClick={() => auth.mfaSmsUnenroll()}>Unenroll SMS</Button>
    //   )}

    //   <Button onClick={() => auth.signOut()}>Logout</Button>
    // </div>
  );
};

export default LoginComponent;
