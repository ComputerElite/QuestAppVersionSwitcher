import { styled } from "@suid/material";
import { Typography as MuiTypography } from "@suid/material";

// Styles
export const SmallText = styled(MuiTypography)({
    fontFamily: 'Roboto',
    fontStyle: 'normal',
    fontWeight: 400,
    fontSize: '12px',
    lineHeight: '16px',
    color: '#FFFFFF',
    marginBottom: '5px',
});

export const MediumText = styled(MuiTypography)({
    fontFamily: 'Roboto',
    fontStyle: 'normal',
    fontWeight: 400,
    fontSize: '14px',
    lineHeight: '16px',
    color: '#FFFFFF',
    marginBottom: '5px',
});

export const TitleText = styled(MuiTypography)({
    fontFamily: 'Roboto',
    fontStyle: 'normal',
    fontWeight: 700,
    fontSize: '18px',
    lineHeight: '21px',
    color: '#F9FAFB',
    marginBottom: '5px',
});