# Headless Auth React

## What is it?

Headless auth react provides a standard and easy method of interacting with different auth platforms in react.

## Simple Example

Below is a simple email+password login with social auth example showcasing how headless auth works.

```
import { useAuth } from "@ixeta/headless-auth-react";

export default function Login() {
  const auth = useAuth();

  if (!auth.isReady) {
    return <>Loading...</>;
  }

  // The user is not logged in or a re-login is required for MFA
  if (!auth.isLoggedIn) {
    if (auth.view === "login") {
      return (
        <div className="w-full h-full flex justify-center items-center">
          <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
            <div className="text-red-800">{auth.error}</div>
            <input
              type="text"
              placeholder="Email"
              value={auth.emailAddress}
              onChange={(e) => auth.setEmailAddress(e.currentTarget.value)}
            />
            <input
              placeholder="Password"
              value={auth.password}
              onChange={(e) => auth.setPassword(e.currentTarget.value)}
            />
            <button onClick={() => auth.signIn()}>Login</button>
            <button onClick={() => auth.signInProvider("google")}>
              Google Sign In
            </button>
            <button onClick={() => auth.signInProvider("facebook")}>
              Facebook Sign In
            </button>
            <a
              className="w-full flex justify-center cursor-pointer"
              onClick={() => auth.setView("register")}
            >
              Register an Account
            </a>
          </div>
        </div>
      );
    }
    if (auth.view === "register") {
      return (
        <div className="w-full h-full flex justify-center items-center">
          <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
            <div className="text-red-800">{auth.error}</div>
            <input
              placeholder="Email"
              value={auth.emailAddress}
              onChange={(e) => auth.setEmailAddress(e.currentTarget.value)}
            />
            <input
              placeholder="Password"
              value={auth.password}
              onChange={(e) => auth.setPassword(e.currentTarget.value)}
            />
            <button onClick={auth.signUp}>Sign Up</button>
            <a
              className="w-full flex justify-center cursor-pointer"
              onClick={() => auth.setView("login")}
            >
              Login
            </a>
          </div>
        </div>
      );
    }
  }

  if (!auth.isEmailVerified) {
    return (
      <div className="w-full h-full flex justify-center items-center">
        <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
          <div className=" text-red-800">{auth.error}</div>
          Your email isn&apos;t verified.
          <button
            onClick={async () => {
              // Send verification email
            }}
          >
            Resend Verification
          </button>
        </div>
      </div>
    );
  }

  // Login success
  // Route to the apps dashboard\internal landing page
  return (
    <div className="w-full h-full flex justify-center items-center flex-col gap-2">
      <button onClick={() => auth.signOut()}>Logout</button>
    </div>
  );
}

```

## How it works

Easily create all of your login screens without worrying about the underlying implementation. The below example show's how you could create all of the views\screens for a complete login experience with TOTP and SMS MFA.

```
export default function Login() {
  const auth = useAuth();

  if (!auth.isReady) {
    return <Loading />;
  }

  // User is attempting to login and MFA is required
  if (!auth.isLoggedIn && auth.isMfaRequired) {
    if (auth.view !== "mfa_totp" && auth.view !== "mfa_sms") {
      return ...
    }
    // Prompt the user for a totp code
    if (auth.view === "mfa_totp") {
      return ...
    }
    // Prompt the user for a sms code
    if (auth.view === "mfa_sms") {
      return ...
    }
  }

  // The user is not logged in or a re-login is required for MFA
  if (!auth.isLoggedIn || auth.isReLoginRequired) {
    if (auth.view === "login") {
      return ...
    }
    if (auth.view === "register") {
      return ...
    }
  }

  // User's email hasn't been verified
  if (!auth.isEmailVerified) {
    return ...
  }

  // If the user isn't enrolled in at least 1 mfa
  if (!(auth.mfaTotpEnrolled || auth.mfaSmsEnrolled)) {
    if (
      auth.view !== "setup_mfa_totp" &&
      auth.view !== "setup_mfa_sms" &&
      auth.view !== "setup_mfa_sms_enroll"
    ) {
      return ...
    }
    // Show QR code for authenticator app
    if (auth.view === "setup_mfa_totp") {
      return ...
    }
    // Prompt the user for their phone number
    if (auth.view === "setup_mfa_sms") {
      return ...
    }
    // Prompt the user for the code sent to their phone number
    if (auth.view === "setup_mfa_sms_enroll") {
      return ...
    }
  }

  // Login success
  // Route to the apps dashboard\internal landing page
  return ...
}

```

## Simple Example

## Setup

In the root of your react project, setup the auth config with the provider of your choice.

Be sure to include an invisible auth-recaptcha element in the root of your app when allowing SMS MFA.

### Firebase

```
import { initializeApp } from "firebase/app";
import { getAuth } from "firebase/auth";
import { FirebaseAuthConfig } from "@ixeta/headless-auth-react-firebase";
import { AuthProvider } from "@ixeta/headless-auth-react";

export const fireBaseApp = initializeApp(...);
export const fireBaseAuth = getAuth(fireBaseApp);
const firebaseAuthConfig = new FirebaseAuthConfig(fireBaseAuth); // <-- headless auth config

export default function App({ Component, pageProps }: AppProps) {

  // Can configure additional options
  firebaseAuthConfig.setOptions({
    totpAppName: "Viffy",
    onSignUpSuccess: async (authConfig) => {
      // On Sign up success, can send verification email
    },
    onSignInSuccess: async () => {},
    onSignOutSuccess: async () => {},
  });

  return (
	<AuthProvider authConfig={firebaseAuthConfig}>
		<AppContextProvider>
		  <Component {...pageProps} />
		  <div id="auth-recaptcha" className="invisible" />
		</AppContextProvider>
	</AuthProvider>
  );
}
```

### Supabase

TODO

## Complete Implementation with Mantine

Below shows a complete implementation that requires a user to setup either TOTP or SMS MFA upon logging in.

```
import { Button, Loader, TextInput } from "@mantine/core";
import QRCode from "react-qr-code";
import { useAuth } from "@ixeta/headless-auth-react";
import Loading from "@/components/common/Loading";
import { sendEmailVerification } from "firebase/auth";
import { fireBaseAuth } from "./_app";

export default function Login() {
  const auth = useAuth();

  if (!auth.isReady) {
    return <Loading />;
  }

  // User is attempting to login and MFA is required
  if (!auth.isLoggedIn && auth.isMfaRequired) {
    if (auth.view !== "mfa_totp" && auth.view !== "mfa_sms") {
      return (
        <div className="w-full h-full flex justify-center items-center">
          <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
            <div>Choose MFA Method</div>
            {/* Get the user enrolled mfa factors */}
            {auth.mfaFactors.map((factor) => {
              if (factor === "totp") {
                return (
                  <Button key={factor} onClick={() => auth.setView("mfa_totp")}>
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
                  >
                    SMS
                  </Button>
                );
              }
              return null;
            })}
            <Button onClick={() => auth.signOut()}>Logout</Button>
          </div>
        </div>
      );
    }
    if (auth.view === "mfa_totp") {
      return (
        <div className="w-full h-full flex justify-center items-center">
          <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
            <div className=" text-red-800">{auth.error}</div>
            <TextInput
              label="MFA Code"
              value={auth.mfaCode}
              onChange={(e) => auth.setMfaCode(e.currentTarget.value)}
            ></TextInput>
            <Button onClick={() => auth.mfaTotpVerify()}>Submit</Button>
          </div>
        </div>
      );
    }
    if (auth.view === "mfa_sms") {
      return (
        <div className="w-full h-full flex justify-center items-center">
          <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
            <div className=" text-red-800">{auth.error}</div>
            <TextInput
              label="MFA Code"
              value={auth.mfaCode}
              onChange={(e) => auth.setMfaCode(e.currentTarget.value)}
            ></TextInput>
            <Button onClick={() => auth.mfaSmsVerify()}>Submit</Button>
          </div>
        </div>
      );
    }
  }

  // The user is not logged in or a re-login is required for MFA
  if (!auth.isLoggedIn || auth.isReLoginRequired) {
    if (auth.view === "login") {
      return (
        <div className="w-full h-full flex justify-center items-center">
          <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
            <div className=" text-red-800">{auth.error}</div>
            <TextInput
              label="Email"
              value={auth.emailAddress}
              onChange={(e) => auth.setEmailAddress(e.currentTarget.value)}
            />
            <TextInput
              label="Password"
              value={auth.password}
              onChange={(e) => auth.setPassword(e.currentTarget.value)}
            />
            <Button onClick={() => auth.signIn()}>Login</Button>
            <Button onClick={() => auth.signInProvider("google")}>
              Google Sign In
            </Button>
            <Button onClick={() => auth.signInProvider("facebook")}>
              Facebook Sign In
            </Button>
            <a
              className="w-full flex justify-center cursor-pointer"
              onClick={() => auth.setView("register")}
            >
              Register an Account
            </a>
          </div>
        </div>
      );
    }
    if (auth.view === "register") {
      return (
        <div className="w-full h-full flex justify-center items-center">
          <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
            <div className=" text-red-800">{auth.error}</div>
            <TextInput
              label="Email"
              value={auth.emailAddress}
              onChange={(e) => auth.setEmailAddress(e.currentTarget.value)}
            />
            <TextInput
              label="Password"
              value={auth.password}
              onChange={(e) => auth.setPassword(e.currentTarget.value)}
            />
            <Button onClick={auth.signUp}>Sign Up</Button>
            <a
              className="w-full flex justify-center cursor-pointer"
              onClick={() => auth.setView("login")}
            >
              Login
            </a>
          </div>
        </div>
      );
    }
  }

  if (!auth.isEmailVerified) {
    return (
      <div className="w-full h-full flex justify-center items-center">
        <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
          <div className=" text-red-800">{auth.error}</div>
          Your email isn&apos;t verified.
          <Button
            onClick={async () => {
              if (fireBaseAuth.currentUser === null) {
                return;
              }
              await sendEmailVerification(fireBaseAuth.currentUser);
            }}
          >
            Resend Verification
          </Button>
        </div>
      </div>
    );
  }

  // If the user isn't enrolled in at least 1 mfa
  if (!(auth.mfaTotpEnrolled || auth.mfaSmsEnrolled)) {
    if (
      auth.view !== "setup_mfa_totp" &&
      auth.view !== "setup_mfa_sms" &&
      auth.view !== "setup_mfa_sms_enroll"
    ) {
      return (
        <div className="w-full h-full flex justify-center items-center">
          <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
            Please setup mfa
            <Button
              onClick={async () => {
                auth.setView("setup_mfa_totp");
                auth.mfaTotpCreate();
              }}
            >
              Authenticator App
            </Button>
            <Button onClick={() => auth.setView("setup_mfa_sms")}>SMS</Button>
            <Button onClick={() => auth.signOut()}>Logout</Button>
          </div>
        </div>
      );
    }
    if (auth.view === "setup_mfa_totp") {
      return (
        <div className="w-full h-full flex justify-center items-center">
          <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
            <div className=" text-red-800">{auth.error}</div>
            Use an Authenticator App
            {auth.isProcessing && (
              <div className="w-full h-full flex justify-center items-center">
                <Loader />
              </div>
            )}
            {auth.mfaTotpQrCode && (
              <QRCode
                size={256}
                style={{ height: "auto", maxWidth: "100%", width: "100%" }}
                value={auth.mfaTotpQrCode}
                viewBox={`0 0 256 256`}
              />
            )}
            <TextInput
              label="MFA Code"
              value={auth.mfaCode}
              onChange={(e) => auth.setMfaCode(e.currentTarget.value)}
            ></TextInput>
            <Button onClick={() => auth.mfaTotpEnroll()}>Enroll</Button>
            <Button onClick={() => auth.signOut()}>Logout</Button>
          </div>
        </div>
      );
    }
    if (auth.view === "setup_mfa_sms") {
      return (
        <div className="w-full h-full flex justify-center items-center">
          <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
            <div className=" text-red-800">{auth.error}</div>
            Setup SMS MFA
            <TextInput
              label="Phone Number"
              value={auth.phoneNumber}
              onChange={(e) => auth.setPhoneNumber(e.currentTarget.value)}
            ></TextInput>
            <Button
              onClick={async () => {
                if (await auth.mfaSmsCreate()) {
                  auth.setView("setup_mfa_sms_enroll");
                }
              }}
            >
              Send Code
            </Button>
            <Button onClick={() => auth.signOut()}>Logout</Button>
          </div>
        </div>
      );
    }
    if (auth.view === "setup_mfa_sms_enroll") {
      return (
        <div className="w-full h-full flex justify-center items-center">
          <div className=" w-52 p-4 border-gray-600 border border-solid rounded flex flex-col gap-4 shadow-md">
            <div className=" text-red-800">{auth.error}</div>
            Setup SMS MFA
            <TextInput
              label="Enter Code"
              value={auth.mfaCode}
              onChange={(e) => auth.setMfaCode(e.currentTarget.value)}
            ></TextInput>
            <Button onClick={() => auth.mfaSmsEnroll()}>Enroll</Button>
            <Button onClick={() => auth.mfaSmsCreate()}>Resend Code</Button>
            <Button onClick={() => auth.signOut()}>Logout</Button>
          </div>
        </div>
      );
    }
  }

  // Login success
  // Route to the apps dashboard\internal landing page
  return (
    <div className="w-full h-full flex justify-center items-center flex-col gap-2">
      {auth.mfaTotpEnrolled && (
        <Button onClick={() => auth.mfaTotpUnenroll()}>Unenroll TOTP</Button>
      )}
      {auth.mfaSmsEnrolled && (
        <Button onClick={() => auth.mfaSmsUnenroll()}>Unenroll SMS</Button>
      )}

      <Button onClick={() => auth.signOut()}>Logout</Button>
    </div>
  );
}
```
