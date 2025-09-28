import { useAuth } from "@ixeta/headless-auth-react";
import { Button, TextInput, Checkbox, Loader } from "@mantine/core";
import { useRouter } from "next/router";
import { sendEmailVerification } from "firebase/auth";
import React from "react";
import { firebaseAuth } from "@/pages/_app";

const LoginComponent = () => {
  const router = useRouter();
  const auth = useAuth();

  if (!auth.isReady) {
    return (
      <div className="w-full h-full flex justify-center items-center">
        <Loader />
      </div>
    );
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
            <Checkbox
              label="Remember Me"
              checked={auth.remember}
              onChange={(e) => {
                auth.setRemember(e.currentTarget.checked);
              }}
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
              if (firebaseAuth == null) {
                return;
              }
              if (firebaseAuth.currentUser === null) {
                return;
              }
              await sendEmailVerification(firebaseAuth.currentUser);
            }}
          >
            Resend Verification
          </Button>
        </div>
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
