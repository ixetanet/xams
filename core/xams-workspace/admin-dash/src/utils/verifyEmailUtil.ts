export const verifyEmail = async (email: string) => {
  const resp = await fetch(`${process.env.NEXT_PUBLIC_API}/xams/verifyemail`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ email }),
  });
  if (!resp.ok) {
    console.error("Error verifying email:", resp.statusText);
    return null;
  }
  return null;
};
