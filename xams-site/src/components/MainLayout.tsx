import React from "react";
import Footer from "./Footer";
import Header from "./Header";

interface MainLayoutProps {
  children: React.ReactNode;
}

const MainLayout = (props: MainLayoutProps) => {
  return (
    <div className="w-full h-ful ">
      <Header />
      {props.children}
      <Footer />
    </div>
  );
};

export default MainLayout;
