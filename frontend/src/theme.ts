
import { createTheme, ThemeProvider } from "@suid/material/styles";


export const theme = createTheme({
    components: {
        // Name of the component ⚛️
        MuiButtonBase: {
            defaultProps: {
                // The default props to change
                disableRipple: true, // No more ripple, on the whole application 💣!
            },
        },
    },
});