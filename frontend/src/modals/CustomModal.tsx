import { Modal, Box, IconButton, Typography } from "@suid/material";
import { splitProps, JSX, children, Show } from "solid-js";
import { FiX } from "solid-icons/fi";

type ModalProps = {
  open: boolean;
  onClose?: () => void;
  onBackdropClick?: () => void;
  offsetLess?: boolean;
  hideCloseButton?: boolean;
  limitHeight?: boolean;
  title?: string;
  children: JSX.Element;
  buttons?: JSX.Element;
};
export const CustomModal = (props: ModalProps) => {
  const [local, rest] = splitProps(props, [
    "open",
    "onClose",
    "onBackdropClick",
    "children",
    "hideCloseButton",
    "title",
    "offsetLess",
    "buttons",
    "limitHeight",
  ]);

  const content = children(() => local.children);
  const buttons = children(() => local.buttons);
  return (
    <Modal
      open={local.open}
      onClose={local.onClose}
      onBackdropClick={local.onBackdropClick}
      BackdropProps={{
        sx: {
          backdropFilter: "blur(10px)",
          maxHeight: (theme) => (local.limitHeight ? "100vh" : "unset"),
        },
      }}
      {...rest}
    >
      <Box
        sx={{
          position: "absolute",
          display: "flex",
          top: "50%",
          left: "50%",
          transform: "translate(-50%, -50%)",
          // width: 400,
          bgcolor: "#111827",
          flexDirection: "column",
          maxHeight: "100vh",
          overflowY: "auto",
          borderRadius: "4px",
          // Shadow material
          boxShadow: "0px 4px 24px rgba(0, 0, 0, 0.5)",
        }}
      >
        <Box
          sx={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",

            px: 3,
            py: 1,
            pr: 1,
            pb: 0,
          }}
        >
          <Typography variant="h6" fontSize={"16px"}>
            {local.title}
          </Typography>
          <Show when={!local.hideCloseButton}>
            <IconButton
              onClick={props.onClose}
              sx={{
                zIndex: 2,
                color: (theme) => theme.palette.grey[500],
              }}
            >
              {" "}
              <FiX />
            </IconButton>
          </Show>
        </Box>
        <Box
          sx={{
            px: local.offsetLess ? 0 : 3,
            pb: local.offsetLess ? 0 : 2,
            pt: local.offsetLess ? 0 : 0,
            overflowY: "auto",
          }}
        >
          {content()}
        </Box>
        <Show when={local.buttons}>
          <Box
            sx={{
              // px: 0,
              pb: 2,
              // minWidth: "300px",
            }}
          >
            <Box
              sx={{
                display: "flex",
                justifyContent: "flex-end",
                // mt: 2,
                gap: 1,
                px: 3,
              }}
            >
              {buttons()}
            </Box>
          </Box>
        </Show>
      </Box>
    </Modal>
  );
};
