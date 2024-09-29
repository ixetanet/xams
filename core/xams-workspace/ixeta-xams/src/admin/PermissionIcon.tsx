import React from "react";
import { PermissionLevels } from "../hooks/useAdminPermission";

interface PermissionIconProps {
  icon: PermissionLevels;
  onClick?: () => void;
}

const PermissionIcon = (props: PermissionIconProps) => {
  return (
    <div className="flex justify-center items-center w-full h-full">
      {props.icon === "none" && (
        <svg
          width="20"
          height="20"
          viewBox="0 0 32 32"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          className="cursor-pointer"
          onClick={props.onClick}
        >
          <rect
            x="2.00024"
            y="2"
            width="28"
            height="28"
            rx="6"
            stroke="#4B4B4B"
            strokeWidth="4"
          />
        </svg>
      )}
      {props.icon === "user" && (
        <svg
          width="20"
          height="20"
          viewBox="0 0 32 32"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          className="cursor-pointer"
          onClick={props.onClick}
        >
          <rect
            x="2.00024"
            y="2"
            width="28"
            height="28"
            rx="6"
            stroke="#4B4B4B"
            strokeWidth="4"
          />
          <mask
            id="mask0_712_5305"
            // style="mask-type:alpha"
            maskUnits="userSpaceOnUse"
            x="4"
            y="16"
            width="12"
            height="12"
          >
            <path d="M4 16V28H16L4 16Z" fill="#D9D9D9" />
          </mask>
          <g mask="url(#mask0_712_5305)">
            <rect
              x="4.00024"
              y="4"
              width="24"
              height="24"
              rx="4"
              fill="#991B1B"
            />
          </g>
        </svg>
      )}
      {props.icon === "team" && (
        <svg
          width="20"
          height="20"
          viewBox="0 0 32 32"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          className="cursor-pointer"
          onClick={props.onClick}
        >
          <rect
            x="2"
            y="2"
            width="28"
            height="28"
            rx="6"
            stroke="#4B4B4B"
            strokeWidth="4"
          />
          <mask
            id="mask0_712_5300"
            // style="mask-type:alpha"
            maskUnits="userSpaceOnUse"
            x="4"
            y="4"
            width="24"
            height="24"
          >
            <path d="M4 28H28L4 4V28Z" fill="#D9D9D9" />
          </mask>
          <g mask="url(#mask0_712_5300)">
            <rect x="4" y="4" width="24" height="24" rx="4" fill="#F59E0B" />
          </g>
        </svg>
      )}
      {props.icon === "system" && (
        <svg
          width="20"
          height="20"
          viewBox="0 0 32 32"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          className="cursor-pointer"
          onClick={props.onClick}
        >
          <rect
            x="2"
            y="2"
            width="28"
            height="28"
            rx="6"
            stroke="#4B4B4B"
            strokeWidth="4"
          />
          <rect x="4" y="4" width="24" height="24" rx="4" fill="#84CC16" />
        </svg>
      )}
    </div>
  );
};

export default PermissionIcon;
