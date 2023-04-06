import { Box, Button, Modal, Typography } from "@suid/material";
import { createEffect, createSignal } from "solid-js"
import { createStore, } from "solid-js/store";

interface ConfirmModalState {
  isOpen: boolean;
  title?: string;
  message?: string;
  onOk?: () => void;
  onCancel?: () => void;
  okText: string;
  cancelText: string;
}


// TODO: Fix this to work on reload in dev mode
const [confirmState, setConfrmState] = createStore<ConfirmModalState>({
  isOpen: false,
  title: "Confirm your action",
  message: "Are you sure you want to do thisasdfsfadsfsadfsdaf dsfadsf sadf saf saf asdf adsfads dsfasfsadf as fdsa adsfadsf sadf adsf adsf dsaf sadf adsf adsf adsf adsfadsf adsf adsf s fd df sd df sdf sdf sdf dsf dfs fdvfgdsfasdfs fas safas fadsfsad fa sfasdf adsfsadf dsfd?",
  onOk: () => { },
  onCancel: () => { },
  okText: "Ok",
  cancelText: "Cancel",
});

interface ConfirmModalProps {
  title?: string;
  message?: string;
  okText: string;
  cancelText: string;
}


export function hideConfirmModal() {
  // Reset state
  setConfrmState({
    isOpen: false,
    cancelText: "Cancel",
    okText: "Ok",
    title: "Confirm your action",
    message: "Are you sure you want to do this?",
    onCancel: () => { },
    onOk: () => { }
  })
}

export async function showConfirmModal(state: Partial<ConfirmModalProps>): Promise<boolean> {
  return new Promise((resolve, reject) => {
    function onOk() {
      resolve(true);
    }
    function onCancel() {
      resolve(false);
    }

    setConfrmState({
      ...state,
      isOpen: true,
      onOk,
      onCancel,
      okText: state.okText ?? "Ok",
      cancelText: state.cancelText ?? "Cancel",
      title: state.title ?? "Confirm your action",
      message: state.message ?? "Are you sure you want to do this?",

    });
  });
}


export default function ConfirmModal() {

  createEffect(() => {
    console.log("confirm state changed", confirmState);
  })

  function onCancel() {
    if (confirmState.onCancel)
      confirmState.onCancel();
    hideConfirmModal();
  }

  function onOk() {
    if (confirmState.onOk)
      confirmState.onOk();
    hideConfirmModal();
  }

  return (
    <Modal
      open={confirmState?.isOpen }
      onClose={() => { onCancel }}
      onBackdropClick={onCancel}
      BackdropProps={{
        sx: {
          backdropFilter: "blur(10px)",
        }
      }}
    >
      <Box
        sx={{
          position: "absolute",
          top: "50%",
          left: "50%",
          p: 3,
          transform: "translate(-50%, -50%)",
          maxWidth: "80vw",
          bgcolor: "#222",
          boxShadow: "24px",
          display: "flex",
          flexDirection: "column",
          maxHeight: "80vh",
        }}
      >
        <Typography id="modal-modal-title" variant="h6" component="h2">
          {confirmState?.title}
        </Typography>
        <Box sx={{ mt: 2, overflowX: "auto", pb: 3 }}>
          <Typography>{confirmState?.message}</Typography>
        </Box>

        <Box sx={{
          display: "flex",
          justifyContent: "space-between"
        }}>
          <Button color="error" onClick={onCancel}>Cancel</Button>
          <Button color="success" onClick={onOk}>Ok</Button>
        </Box>
      </Box>
    </Modal>
  )
}