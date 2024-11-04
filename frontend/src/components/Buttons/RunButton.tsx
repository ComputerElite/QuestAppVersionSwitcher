import { Component, JSX, ParentComponent, Show, splitProps } from "solid-js";
import style from "./RunButton.module.scss";

type RunButtonProps = JSX.HTMLAttributes<HTMLButtonElement> & {
  onClick?: () => void;
  icon?: JSX.Element;
  text?: string;
  variant?: "success" | "error" | "warning" | "info";
  disabled?: boolean;
  type?: "button" | "submit" | "reset";
  hideTextOnMobile?: boolean;
  fullWidth?: boolean;
};

const RunButton: Component<RunButtonProps> = (props) => {
  let [local, other] = splitProps(props, [
    "children",
    "icon",
    "text",
    "variant",
    "hideTextOnMobile",
    "fullWidth",
    "classList",
  ]);

  return (
    <button
      classList={{
        [style.button]: true, // set this here so we can override it in style prop
        [style.success]: local.variant === "success",
        [style.error]: local.variant === "error",
        [style.warning]: local.variant === "warning",
        [style.info]: local.variant === "info",
        [style.textOnly]: !local.icon,
        [style.iconOnly]: !local.text,
        [style.hideTextOnMobile]: local.hideTextOnMobile,
        [style.fullWidth]: local.fullWidth,
        ...local.classList,
      }}
      {...other}
    >
      <Show when={local.icon}>
        <div class={style.icon}>{local.icon}</div>
      </Show>
      <Show when={local.text}>
        <span class={style.text}>{local.text}</span>
      </Show>
    </button>
  );
};

export default RunButton;
