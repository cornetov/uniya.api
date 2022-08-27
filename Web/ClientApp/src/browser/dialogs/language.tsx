/* eslint-disable jsx-a11y/anchor-is-valid */

import * as React from 'react';
//import { observer, inject } from 'mobx-react';
import { Localizer } from '../model/localizer';
//import { string } from 'prop-types';
//import { values } from 'mobx';

export interface LanguageProps {
    modalState: boolean,
    closeModal: any
}
export interface LanguageState {
    loading: boolean;
    lang: string;
}

export class Language extends React.Component<LanguageProps, LanguageState> {
    //static displayName = Home.name;

    localizer: Localizer;
    //state: {};

    constructor(props: LanguageProps) {
        super(props);

        this.localizer = new Localizer("Language");

        this.state = {
            loading: true, lang: ""
        };

        this.changeLanguage = this.changeLanguage.bind(this);
        this.renderContent = this.renderContent.bind(this);
    }

    async componentDidMount() {

        // load localized terms
        try {
            await this.localizer.load();
            this.setState({ loading: false, lang: this.localizer.term("Current") });
        } catch (error) {
            console.log(error);
        }
    }

    changeLanguage() {
        //this.setState({ loading: this.state.loading, lang: e.currentTarget.values });
        this.props.closeModal();
    }

    //changeLanguage() => e {
    //    const { name, value } = e.target;

    //    this.setState({
    //        [name]: value
    //    });
    //};
    //public isChecked(key: string): boolean {

    //    const current = "ru-RU";
    //    return key.startsWith("ru");
    //}

    renderContent() {

        //const current = localizer.term("Current");
        const terms = this.localizer.terms("@");
        let keys = [...terms.keys()];

        //const langs = new Set<string>();
        //langs.add(this.state.lang);
        //  onChange={this.changeLanguage.bind(this)}

        return (
            <table>
                <thead>
                    <tr>
                        <th colSpan={2}>{this.localizer.term("Select")}</th>
                    </tr>
                </thead>
                <tbody>
                    {keys.map(key =>
                        <tr key={key}>
                            <td>&nbsp;</td>
                            <td>
                                <label className="radio"><input type="radio" name="lang" value={key} defaultChecked={key === this.state.lang} /></label>
                                <span>&nbsp;</span><span>{terms.get(key)}</span>
                            </td>
                        </tr>
                    )}
                </tbody>
            </table>
        );
    }

    render() {
        if (!this.props.modalState) {
            return null;        // hide!
        }

        let content = this.state.loading
            ? <progress className="progress is-small is-primary" max="100%">30%</progress>
            : this.renderContent();

        return (
            <div className="modal is-active">
                <div className="modal-background" onClick={this.props.closeModal}></div>
                <div className="modal-card">
                    <header className="modal-card-head">
                        <p className="modal-card-title">{Localizer.term("Language")}</p>
                        <button className="delete" aria-label="close" onClick={this.props.closeModal}></button>
                    </header>
                    <section className="modal-card-body">
                        {content}
                    </section>
                    <footer className="modal-card-foot">
                        <a className="button is-success" onClick={this.changeLanguage}>{Localizer.term("Apply")}</a>
                        <a className="button" onClick={this.props.closeModal}>{Localizer.term("Cancel")}</a>
                    </footer>
                </div>
            </div>
        );
    }
}

//export default inject('rootStore')(observer(Language));