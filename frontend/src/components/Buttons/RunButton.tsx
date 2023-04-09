import { Component, JSX, ParentComponent, Show, splitProps } from 'solid-js';
import style from './RunButton.module.scss'


type RunButtonProps = JSX.HTMLAttributes<HTMLButtonElement> & {
    onClick?: () => void;
    icon?: JSX.Element;
    text?: string;
    variant?: 'success' | 'error' | 'warning' | 'info';
}

const RunButton: Component<RunButtonProps> = (props) => {
    let [local, other] = splitProps(props, ['children', 'icon', 'text', 'variant'])


    return (
        <button class={style.button} classList={{
            [style.success]: local.variant === 'success',
            [style.error]: local.variant === 'error',
            [style.warning]: local.variant === 'warning',
            [style.info]: local.variant === 'info',
            [style.textOnly]: !local.icon,
            [style.iconOnly]: !local.text,
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