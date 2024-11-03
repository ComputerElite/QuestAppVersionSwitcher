
import { Component, JSX } from "solid-js"


import "./DialogModal.scss"
interface DialogModalProps {
    heading: string;
    children: JSX.Element;
}

export default function DialogModal(props: DialogModalProps) {

    return (
        <div
            role="presentation"
            class="modalBackdrop"
            onClick={()=> {
                console.log("modal should close")
            }}
            onKeyPress={()=> console.log("modal should close")}
        >
            <section role="dialog" class="modal">
                <header>
                    <h2>{props.heading?? "Nothing"}</h2>
                </header>
                <div class="modalBody">
                    {props.children}
                </div>
            </section>
        </div>
    )
}