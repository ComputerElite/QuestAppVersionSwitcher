import { ParentComponent, children, splitProps, JSX } from 'solid-js'
import styles from './PageLayout.module.scss'

type PageLayoutProps = JSX.HTMLAttributes<HTMLDivElement> & {
    hasOffset?: boolean;
    fullWidth?: boolean;
}

const PageLayout: ParentComponent<PageLayoutProps> = (props) => {
    let [local, other] = splitProps(props, ['children', 'hasOffset', 'fullWidth'])

    let content = children(() => local.children);

    return (
        <div classList={{
            [styles.pageLayout]: true,
            [styles.offset]: local.hasOffset ?? true,
            [styles.fullWidth]: local.fullWidth ?? false
        }}
            {...other}
        >
            {content()}
        </div>
    )
}

export default PageLayout