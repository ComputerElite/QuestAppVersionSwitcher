import { Portal } from 'solid-js/web'
import { children } from 'solid-js';

export default function ModalContainer({ass}: any) {
  let element = children(()=> ass);
  
    return (
    <Portal>
        {element()}
        <div>ModalContainer</div>
    </Portal>
  )
}
