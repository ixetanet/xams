import { Burger, Drawer } from "@mantine/core";
import { useDisclosure } from "@mantine/hooks";
import { useRouter } from "next/router";
import React, { Dispatch, SetStateAction } from "react";

export type Example = {
  title: string;
  component: JSX.Element;
  codeComponent: JSX.Element;
  id: number;
};
interface NavigationProps {
  formExamples: Example[];
  formFieldsExamples: Example[];
  dataTableExamples: Example[];
  dataTableFormExamples: Example[];
}

interface NavLinkProps {
  example: Example;
  close: () => void;
}

const NavLink = (props: NavLinkProps) => {
  const router = useRouter();

  return (
    <div
      className="cursor-pointer"
      onClick={() => {
        router.push(`/?id=${props.example.id}`);
        props.close();
      }}
    >
      {props.example.title}
    </div>
  );
};

const Navigation = (props: NavigationProps) => {
  const [opened, handlers] = useDisclosure(false);
  const label = opened ? "Close navigation" : "Open navigation";

  return (
    <>
      <div className="flex items-center gap-2">
        <Burger opened={opened} onClick={handlers.open} aria-label={label} />
        Navigation
      </div>
      <Drawer opened={opened} onClose={handlers.close} size="xs" title="">
        <div className="font-bold">Forms</div>
        {props.formExamples.map((example, index) => (
          <NavLink key={index} example={example} close={handlers.close} />
        ))}
        <div className="font-bold mt-4">Form Fields</div>
        {props.formFieldsExamples.map((example, index) => (
          <NavLink key={index} example={example} close={handlers.close} />
        ))}
        <div className="font-bold mt-4">DataTable</div>
        {props.dataTableExamples.map((example, index) => (
          <NavLink key={index} example={example} close={handlers.close} />
        ))}
        <div className=" font-bold mt-4">DataTable Form</div>
        {props.dataTableFormExamples.map((example, index) => (
          <NavLink key={index} example={example} close={handlers.close} />
        ))}
      </Drawer>
    </>
  );
};

export default Navigation;
