import { JSX } from 'solid-js/web/types/jsx';
import style from './Button.module.scss';
import { splitProps, Component, children } from 'solid-js';

interface ButtonProps {
    onClick?: () => void;
    disabled?: boolean;
    type?: 'button' | 'submit' | 'reset';
    classList?: Record<string, boolean>;
    class?: string;
    children?: any;
    icon?: JSX.Element;
    fullWidth?: boolean;
}


export const Button: Component<ButtonProps> = (props) => {
    let [local, others] = splitProps(props, [
        "children",
        "classList",
        "icon",
        "fullWidth",
        
    ]);

    let content = children(()=> local.children);

    return (
        <button classList={
            {
                [style.button]: true,
                [style.fullwidth]: local.fullWidth,
                ...local.classList
            }
        } {...others}>
            <div>
               {props.icon} {content()}
            </div>
        </button>
    )
}