import { useAuth } from "@ixeta/headless-auth-react";
import { Button, TextInput, Checkbox, Loader, Paper, Title, Text, Divider, Stack } from "@mantine/core";
import { useRouter } from "next/router";
import { sendEmailVerification } from "firebase/auth";
import React from "react";
import { firebaseAuth } from "@/pages/_app";

const LoginComponent = () => {
  const router = useRouter();
  const auth = useAuth();

  if (!auth.isReady) {
    return (
      <div className="min-h-screen bg-gray-50 flex justify-center items-center">
        <Loader size="md" />
      </div>
    );
  }

  // User is attempting to login and MFA is required
  if (!auth.isLoggedIn && auth.isMfaRequired) {
    if (auth.view !== "mfa_totp" && auth.view !== "mfa_sms") {
      return (
        <div className="min-h-screen bg-gray-50 flex justify-center items-center p-4">
          <Paper
            shadow="sm"
            radius="lg"
            className="w-full max-w-sm"
            styles={{
              root: {
                padding: '2rem',
                border: '1px solid #e9ecef'
              }
            }}
          >
            <Stack spacing="md">
              <Title order={3} className="text-center text-gray-900 font-medium">
                Choose verification method
              </Title>
              {/* Get the user enrolled mfa factors */}
              {auth.mfaFactors.map((factor) => {
                if (factor === "totp") {
                  return (
                    <Button
                      key={factor}
                      onClick={() => auth.setView("mfa_totp")}
                      variant="outline"
                      size="md"
                      className="h-12"
                      styles={{
                        root: {
                          border: '1px solid #dee2e6',
                          color: '#495057',
                          '&:hover': {
                            backgroundColor: '#f8f9fa',
                            border: '1px solid #adb5bd'
                          }
                        }
                      }}
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
                      variant="outline"
                      size="md"
                      className="h-12"
                      styles={{
                        root: {
                          border: '1px solid #dee2e6',
                          color: '#495057',
                          '&:hover': {
                            backgroundColor: '#f8f9fa',
                            border: '1px solid #adb5bd'
                          }
                        }
                      }}
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
                className="mt-2"
                styles={{
                  root: {
                    color: '#6c757d',
                    '&:hover': {
                      backgroundColor: '#f8f9fa'
                    }
                  }
                }}
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
        <div className="min-h-screen bg-gray-50 flex justify-center items-center p-4">
          <Paper
            shadow="sm"
            radius="lg"
            className="w-full max-w-sm"
            styles={{
              root: {
                padding: '2rem',
                border: '1px solid #e9ecef'
              }
            }}
          >
            <Stack spacing="lg">
              <div className="text-center">
                <Title order={3} className="text-gray-900 font-medium mb-2">
                  Enter verification code
                </Title>
                <Text size="sm" className="text-gray-600">
                  Enter the code from your authenticator app
                </Text>
              </div>
              {auth.error && (
                <Text size="sm" className="text-red-600 text-center bg-red-50 p-3 rounded-md">
                  {auth.error}
                </Text>
              )}
              <TextInput
                label="Verification code"
                placeholder="000000"
                value={auth.mfaCode}
                onChange={(e) => auth.setMfaCode(e.currentTarget.value)}
                size="md"
                styles={{
                  label: {
                    fontSize: '14px',
                    fontWeight: 500,
                    color: '#374151'
                  },
                  input: {
                    height: '48px',
                    fontSize: '16px',
                    border: '1px solid #d1d5db',
                    '&:focus': {
                      borderColor: '#3b82f6',
                      boxShadow: '0 0 0 3px rgba(59, 130, 246, 0.1)'
                    }
                  }
                }}
              />
              <Button
                onClick={() => auth.mfaTotpVerify()}
                size="md"
                className="h-12"
                styles={{
                  root: {
                    backgroundColor: '#3b82f6',
                    border: 'none',
                    '&:hover': {
                      backgroundColor: '#2563eb'
                    }
                  }
                }}
              >
                Verify
              </Button>
            </Stack>
          </Paper>
        </div>
      );
    }
    if (auth.view === "mfa_sms") {
      return (
        <div className="min-h-screen bg-gray-50 flex justify-center items-center p-4">
          <Paper
            shadow="sm"
            radius="lg"
            className="w-full max-w-sm"
            styles={{
              root: {
                padding: '2rem',
                border: '1px solid #e9ecef'
              }
            }}
          >
            <Stack spacing="lg">
              <div className="text-center">
                <Title order={3} className="text-gray-900 font-medium mb-2">
                  Enter SMS code
                </Title>
                <Text size="sm" className="text-gray-600">
                  Enter the code sent to your phone
                </Text>
              </div>
              {auth.error && (
                <Text size="sm" className="text-red-600 text-center bg-red-50 p-3 rounded-md">
                  {auth.error}
                </Text>
              )}
              <TextInput
                label="SMS code"
                placeholder="000000"
                value={auth.mfaCode}
                onChange={(e) => auth.setMfaCode(e.currentTarget.value)}
                size="md"
                styles={{
                  label: {
                    fontSize: '14px',
                    fontWeight: 500,
                    color: '#374151'
                  },
                  input: {
                    height: '48px',
                    fontSize: '16px',
                    border: '1px solid #d1d5db',
                    '&:focus': {
                      borderColor: '#3b82f6',
                      boxShadow: '0 0 0 3px rgba(59, 130, 246, 0.1)'
                    }
                  }
                }}
              />
              <Button
                onClick={() => auth.mfaSmsVerify()}
                size="md"
                className="h-12"
                styles={{
                  root: {
                    backgroundColor: '#3b82f6',
                    border: 'none',
                    '&:hover': {
                      backgroundColor: '#2563eb'
                    }
                  }
                }}
              >
                Verify
              </Button>
            </Stack>
          </Paper>
        </div>
      );
    }
  }

  // The user is not logged in or a re-login is required for MFA
  if (!auth.isLoggedIn || auth.isReLoginRequired) {
    if (auth.view === "login") {
      return (
        <div className="min-h-screen bg-gray-50 flex justify-center items-center p-4">
          <Paper
            shadow="sm"
            radius="lg"
            className="w-full max-w-sm"
            styles={{
              root: {
                padding: '2rem',
                border: '1px solid #e9ecef'
              }
            }}
          >
            <Stack spacing="lg">
              <div className="text-center">
                <Title order={2} className="text-gray-900 font-semibold mb-2">
                  Sign in
                </Title>
                <Text size="sm" className="text-gray-600">
                  Welcome back! Please sign in to continue.
                </Text>
              </div>

              {auth.error && (
                <Text size="sm" className="text-red-600 text-center bg-red-50 p-3 rounded-md">
                  {auth.error}
                </Text>
              )}

              <Stack spacing="md">
                <TextInput
                  label="Email address"
                  placeholder="Enter your email"
                  value={auth.emailAddress}
                  onChange={(e) => auth.setEmailAddress(e.currentTarget.value)}
                  size="md"
                  styles={{
                    label: {
                      fontSize: '14px',
                      fontWeight: 500,
                      color: '#374151'
                    },
                    input: {
                      height: '48px',
                      fontSize: '16px',
                      border: '1px solid #d1d5db',
                      '&:focus': {
                        borderColor: '#3b82f6',
                        boxShadow: '0 0 0 3px rgba(59, 130, 246, 0.1)'
                      }
                    }
                  }}
                />
                <TextInput
                  label="Password"
                  placeholder="Enter your password"
                  type="password"
                  value={auth.password}
                  onChange={(e) => auth.setPassword(e.currentTarget.value)}
                  size="md"
                  styles={{
                    label: {
                      fontSize: '14px',
                      fontWeight: 500,
                      color: '#374151'
                    },
                    input: {
                      height: '48px',
                      fontSize: '16px',
                      border: '1px solid #d1d5db',
                      '&:focus': {
                        borderColor: '#3b82f6',
                        boxShadow: '0 0 0 3px rgba(59, 130, 246, 0.1)'
                      }
                    }
                  }}
                />
                <Checkbox
                  label="Remember me"
                  checked={auth.remember}
                  onChange={(e) => {
                    auth.setRemember(e.currentTarget.checked);
                  }}
                  styles={{
                    label: {
                      fontSize: '14px',
                      color: '#374151'
                    }
                  }}
                />
              </Stack>

              <Button
                onClick={() => auth.signIn()}
                size="md"
                className="h-12"
                styles={{
                  root: {
                    backgroundColor: '#3b82f6',
                    border: 'none',
                    '&:hover': {
                      backgroundColor: '#2563eb'
                    }
                  }
                }}
              >
                Sign in
              </Button>

              <Divider label="Or continue with" labelPosition="center" styles={{
                label: {
                  fontSize: '14px',
                  color: '#6b7280'
                }
              }} />

              <Stack spacing="sm">
                <Button
                  onClick={() => auth.signInProvider("google")}
                  variant="outline"
                  size="md"
                  className="h-12"
                  styles={{
                    root: {
                      border: '1px solid #d1d5db',
                      color: '#374151',
                      '&:hover': {
                        backgroundColor: '#f9fafb',
                        border: '1px solid #9ca3af'
                      }
                    }
                  }}
                >
                  Continue with Google
                </Button>
                <Button
                  onClick={() => auth.signInProvider("facebook")}
                  variant="outline"
                  size="md"
                  className="h-12"
                  styles={{
                    root: {
                      border: '1px solid #d1d5db',
                      color: '#374151',
                      '&:hover': {
                        backgroundColor: '#f9fafb',
                        border: '1px solid #9ca3af'
                      }
                    }
                  }}
                >
                  Continue with Facebook
                </Button>
              </Stack>

              <Text size="sm" className="text-center">
                Don't have an account?{' '}
                <a
                  className="text-blue-600 hover:text-blue-500 cursor-pointer font-medium"
                  onClick={() => auth.setView("register")}
                >
                  Sign up
                </a>
              </Text>
            </Stack>
          </Paper>
        </div>
      );
    }
    if (auth.view === "register") {
      return (
        <div className="min-h-screen bg-gray-50 flex justify-center items-center p-4">
          <Paper
            shadow="sm"
            radius="lg"
            className="w-full max-w-sm"
            styles={{
              root: {
                padding: '2rem',
                border: '1px solid #e9ecef'
              }
            }}
          >
            <Stack spacing="lg">
              <div className="text-center">
                <Title order={2} className="text-gray-900 font-semibold mb-2">
                  Create account
                </Title>
                <Text size="sm" className="text-gray-600">
                  Get started by creating your account.
                </Text>
              </div>

              {auth.error && (
                <Text size="sm" className="text-red-600 text-center bg-red-50 p-3 rounded-md">
                  {auth.error}
                </Text>
              )}

              <Stack spacing="md">
                <TextInput
                  label="Email address"
                  placeholder="Enter your email"
                  value={auth.emailAddress}
                  onChange={(e) => auth.setEmailAddress(e.currentTarget.value)}
                  size="md"
                  styles={{
                    label: {
                      fontSize: '14px',
                      fontWeight: 500,
                      color: '#374151'
                    },
                    input: {
                      height: '48px',
                      fontSize: '16px',
                      border: '1px solid #d1d5db',
                      '&:focus': {
                        borderColor: '#3b82f6',
                        boxShadow: '0 0 0 3px rgba(59, 130, 246, 0.1)'
                      }
                    }
                  }}
                />
                <TextInput
                  label="Password"
                  placeholder="Create a password"
                  type="password"
                  value={auth.password}
                  onChange={(e) => auth.setPassword(e.currentTarget.value)}
                  size="md"
                  styles={{
                    label: {
                      fontSize: '14px',
                      fontWeight: 500,
                      color: '#374151'
                    },
                    input: {
                      height: '48px',
                      fontSize: '16px',
                      border: '1px solid #d1d5db',
                      '&:focus': {
                        borderColor: '#3b82f6',
                        boxShadow: '0 0 0 3px rgba(59, 130, 246, 0.1)'
                      }
                    }
                  }}
                />
              </Stack>

              <Button
                onClick={auth.signUp}
                size="md"
                className="h-12"
                styles={{
                  root: {
                    backgroundColor: '#3b82f6',
                    border: 'none',
                    '&:hover': {
                      backgroundColor: '#2563eb'
                    }
                  }
                }}
              >
                Create account
              </Button>

              <Text size="sm" className="text-center">
                Already have an account?{' '}
                <a
                  className="text-blue-600 hover:text-blue-500 cursor-pointer font-medium"
                  onClick={() => auth.setView("login")}
                >
                  Sign in
                </a>
              </Text>
            </Stack>
          </Paper>
        </div>
      );
    }
  }

  if (!auth.isEmailVerified) {
    return (
      <div className="min-h-screen bg-gray-50 flex justify-center items-center p-4">
        <Paper
          shadow="sm"
          radius="lg"
          className="w-full max-w-sm"
          styles={{
            root: {
              padding: '2rem',
              border: '1px solid #e9ecef'
            }
          }}
        >
          <Stack spacing="lg">
            <div className="text-center">
              <Title order={3} className="text-gray-900 font-medium mb-2">
                Verify your email
              </Title>
              <Text size="sm" className="text-gray-600">
                We've sent a verification link to your email address. Please check your inbox.
              </Text>
            </div>

            {auth.error && (
              <Text size="sm" className="text-red-600 text-center bg-red-50 p-3 rounded-md">
                {auth.error}
              </Text>
            )}

            <Button
              onClick={async () => {
                if (firebaseAuth == null) {
                  return;
                }
                if (firebaseAuth.currentUser === null) {
                  return;
                }
                await sendEmailVerification(firebaseAuth.currentUser);
              }}
              size="md"
              className="h-12"
              styles={{
                root: {
                  backgroundColor: '#3b82f6',
                  border: 'none',
                  '&:hover': {
                    backgroundColor: '#2563eb'
                  }
                }
              }}
            >
              Resend verification email
            </Button>
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
