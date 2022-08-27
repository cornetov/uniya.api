import { marked } from "marked";
import * as React from 'react';
import { Localizer } from '../model/localizer';

export interface HomeProps {
    readme: string
}
export interface HomeState {
    loading: boolean;
    markdown: string;
}

export class Home extends React.Component<HomeProps, HomeState> {
    //static displayName = Home.name;

    constructor(props: HomeProps) {
        super(props);

        this.state = {
            loading: true, markdown: ''
        };
    }

    async setStateAsync(state: HomeState) {
        return new Promise<void>((render) => {
            this.setState(state, render);
        });
    }

    async componentDidMount() {

        // load localized terms
        try {
            await Localizer.load();

            //const markdown = Home.bulma(parse(Localizer.term("Home")));
            const lang = Localizer.term("Readme");

            //const readme = typeof (this.props.readme) === 'string' ? this.props.readme : 'home.md';
            //const markdown = parse(Localizer.term("Home"));
            const response = await fetch(`${lang}/${this.props.readme}`);
            if (!response.ok) {
                throw Error(response.statusText);
            }
            const txt = await response.text();
            const markdown = marked.parse(txt);
            await this.setStateAsync({ loading: false, markdown: markdown });
        } catch (error) {
            console.log(error);
        }
    }

    render() {
        const contents = this.state.loading
            ? <progress className="progress is-small is-primary" max="100%">30%</progress>
            : <div className="content has-text-justified" dangerouslySetInnerHTML={{ __html: this.state.markdown }} />;

        return (
            <div>
                {contents}
            </div>
        );
    }
}