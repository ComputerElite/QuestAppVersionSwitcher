import { Portal } from 'solid-js/web'
import { children } from 'solid-js';
import DialogModal from './DialogModal';
import ChangeGameModal from './ChangeGameModal';
import ConfirmModal from './ConfirmModal';


export default function ModalContainer() {
  return (
    <>
      <ChangeGameModal />
      <ConfirmModal/>
    </>
  )
}