import { Box, Button, Modal, Typography } from "@suid/material";
import { createEffect, createSignal } from "solid-js";
import { createStore } from "solid-js/store";
import { CustomModal } from "./CustomModal";

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
  message:
    "Are you sure you want to do thisasdfsfadsfsadfsdaf dsfadsf sadf saf saf asdf adsfads dsfasfsadf as fdsa adsfadsf sadf adsf adsf dsaf sadf adsf adsf adsf adsfadsf adsf adsf s fd df sd df sdf sdf sdf dsf dfs fdvfgdsfasdfs fas safas fadsfsad fa sfasdf adsfsadf dsfd?",
  onOk: () => {},
  onCancel: () => {},
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
    onCancel: () => {},
    onOk: () => {},
  });
}

export async function showConfirmModal(
  state: Partial<ConfirmModalProps>,
): Promise<boolean> {
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
  function onCancel() {
    if (confirmState.onCancel) confirmState.onCancel();
    hideConfirmModal();
  }

  function onOk() {
    if (confirmState.onOk) confirmState.onOk();
    hideConfirmModal();
  }

  return (
    <CustomModal
      open={confirmState?.isOpen}
      onClose={() => {
        onCancel;
      }}
      onBackdropClick={onCancel}
      buttons={
        <>
          <Button color="error" onClick={onCancel}>
            Cancel
          </Button>
          <Button color="success" onClick={onOk}>
            Ok
          </Button>
        </>
      }
      title={confirmState?.title}
    >
      <Box sx={{ mt: 0, overflowX: "auto", pb: 3 }}>
        <Typography>{confirmState?.message}</Typography>
      </Box>
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
        }}
      ></Box>
    </CustomModal>
  );
}
