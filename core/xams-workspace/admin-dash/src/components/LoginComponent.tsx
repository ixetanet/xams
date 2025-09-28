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
import React from "react";
import { firebaseAuth } from "@/pages/_app";

const LoginComponent = () => {
  const router = useRouter();
  const auth = useAuth();

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
            <form onSubmit={(e) => { e.preventDefault(); auth.mfaTotpVerify(); }}>
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
                />
                <Button type="submit" size="md" fullWidth>
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
            <form onSubmit={(e) => { e.preventDefault(); auth.mfaSmsVerify(); }}>
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
                />
                <Button type="submit" size="md" fullWidth>
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
                  Welcome back! Please sign in to continue.
                </Text>
              </div>

              {auth.error && (
                <Alert color="red" variant="light">
                  {auth.error}
                </Alert>
              )}

              <form onSubmit={(e) => { e.preventDefault(); auth.signIn(); }}>
                <Stack gap="md">
                  <TextInput
                    label="Email address"
                    placeholder="Enter your email"
                    value={auth.emailAddress}
                    onChange={(e) => auth.setEmailAddress(e.currentTarget.value)}
                    size="md"
                  />
                  <TextInput
                    label="Password"
                    placeholder="Enter your password"
                    type="password"
                    value={auth.password}
                    onChange={(e) => auth.setPassword(e.currentTarget.value)}
                    size="md"
                  />
                  <Checkbox
                    label="Remember me"
                    checked={auth.remember}
                    onChange={(e) => {
                      auth.setRemember(e.currentTarget.checked);
                    }}
                  />
                </Stack>

                <Button type="submit" size="md" mt="md" fullWidth>
                  Sign in
                </Button>
              </form>

              <Divider label="Or continue with" labelPosition="center" />

              <Stack gap="sm">
                <Button
                  onClick={() => auth.signInProvider("google")}
                  variant="default"
                  size="md"
                  fullWidth
                >
                  Continue with Google
                </Button>
                <Button
                  onClick={() => auth.signInProvider("facebook")}
                  variant="default"
                  size="md"
                  fullWidth
                >
                  Continue with Facebook
                </Button>
              </Stack>

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

              <form onSubmit={(e) => { e.preventDefault(); auth.signUp(); }}>
                <Stack gap="md">
                  <TextInput
                    label="Email address"
                    placeholder="Enter your email"
                    value={auth.emailAddress}
                    onChange={(e) => auth.setEmailAddress(e.currentTarget.value)}
                    size="md"
                  />
                  <TextInput
                    label="Password"
                    placeholder="Create a password"
                    type="password"
                    value={auth.password}
                    onChange={(e) => auth.setPassword(e.currentTarget.value)}
                    size="md"
                  />
                </Stack>

                <Button type="submit" size="md" mt="md" fullWidth>
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

            <form onSubmit={async (e) => {
              e.preventDefault();
              if (firebaseAuth == null) {
                return;
              }
              if (firebaseAuth.currentUser === null) {
                return;
              }
              await sendEmailVerification(firebaseAuth.currentUser);
            }}>
              <Button type="submit" size="md" fullWidth>
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
