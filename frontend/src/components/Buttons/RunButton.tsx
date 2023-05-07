import { Component, JSX, ParentComponent, Show, splitProps } from 'solid-js';
import style from './RunButton.module.scss'


type RunButtonProps = JSX.HTMLAttributes<HTMLButtonElement> & {
    onClick?: () => void;
    icon?: JSX.Element;
    text?: string;
    variant?: 'success' | 'error' | 'warning' | 'info';
    disabled?: boolean;
    type?: 'button' | 'submit' | 'reset';
    hideTextOnMobile?: boolean;
    fullWidth?: boolean;
}

const RunButton: Component<RunButtonProps> = (props) => {
    let [local, other] = splitProps(props, ['children', 'icon', 'text', 'variant', 'hideTextOnMobile', 'fullWidth'])

    return (
        <button class={style.button} classList={{
            [style.success]: local.variant === 'success',
            [style.error]: local.variant === 'error',
            [style.warning]: local.variant === 'warning',
            [style.info]: local.variant === 'info',
            [style.textOnly]: !local.icon,
            [style.iconOnly]: !local.text,
            [style.hideTextOnMobile]: local.hideTextOnMobile,
            [style.fullWidth]: local.fullWidth
        }} {...other}>
            <Show when={local.icon}>
                <div class={style.icon}>
                    {local.icon}
                </div>
            </Show>
            <Show when={local.text}>
                <div class={style.text}>
                    {local.text}
                </div>
            </Show>
        </button>
    )
}

export default RunButton