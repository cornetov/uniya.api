/* eslint-disable jsx-a11y/anchor-is-valid */

import * as React from 'react';
import { Link } from 'react-router-dom';
import { Localizer } from '../model/localizer';
import { Language } from '../dialogs/language';
//import { Home, HomeProps } from './home';

export interface NavigateProps {
    logoAlt: string;
    logoUrl: string;
}
export interface NavigateState {
    collapsed: boolean;
    loading: boolean;
    actModal: boolean;
    langModal: boolean;
}

export class Navigate extends React.Component<NavigateProps, NavigateState> {

    constructor(props: NavigateProps) {
        super(props);

        this.state = {
            collapsed: true, loading: true, actModal: false, langModal: false
        };

        this.toggleNavbar = this.toggleNavbar.bind(this);
        this.toggleActModal = this.toggleActModal.bind(this);
        this.toggleLangModal = this.toggleLangModal.bind(this);
    }

    async setStateAsync(state: NavigateState) {
        return new Promise<void>((render) => {
            this.setState(state, render);
        });
    }

    async componentDidMount() {

        // load localized terms
        try {
            await Localizer.load();
            await this.setStateAsync({ collapsed: this.state.collapsed, loading: false, actModal: this.state.actModal, langModal: this.state.langModal });
        } catch (error) {
            console.log(error);
        }

        // wait
        //await NavMenu.sleep(100);

        // get all "navbar-burger" elements
        const burgers = document.getElementsByClassName("navbar-burger");
        for (let i = 0; i < burgers.length; i++) {

            const burger = burgers.item(i) as HTMLElement;
            burger.addEventListener('click', () => {

                // get the target from the "data-target" attribute
                const dataTarget = burger.dataset.target as string;
                if (dataTarget !== null) {
                    const target = document.getElementById(dataTarget) as HTMLElement;

                    // toggle the "is-active" class on both the "navbar-burger" and the "navbar-menu"
                    burger.classList.toggle('is-active');
                    target.classList.toggle('is-active');
                }
            });
        }
    }

    toggleActModal() {
        this.setState({
            collapsed: this.state.collapsed,
            loading: this.state.loading,
            actModal: !this.state.actModal,
            langModal: this.state.langModal
        });
    }

    toggleLangModal() {
        this.setState({
            collapsed: this.state.collapsed,
            loading: this.state.loading,
            actModal: this.state.actModal,
            langModal: !this.state.langModal
        });
    }
  
    toggleNavbar() {
        this.setState({
            collapsed: !this.state.collapsed,
            loading: this.state.loading,
            actModal: this.state.actModal,
            langModal: this.state.langModal
        });
    }

    openSwagger() {
        const w = window as Window;
        if (w !== null) {
            w.open('https://localhost:7008/swagger/index.html', '_blank');
        }
    }

    renderNavbar() {
        return (
            <nav className="navbar" role="navigation" aria-label="main navigation">
                <div className="navbar-brand">
                    <Link className="navbar-item" to="/">
                        <img alt={this.props.logoAlt} src={this.props.logoUrl} width="24" height="20" />
                    </Link>
                    <a role="button" id="nav-btn" className="navbar-burger burger" aria-label="menu" aria-expanded="false" data-target="navbarRoot">
                        <span aria-hidden="true" />
                        <span aria-hidden="true" />
                        <span aria-hidden="true" />
                    </a>
                </div>
                <div id="navbarRoot" className="navbar-menu">
                    <div className="navbar-start">
                        <div className="navbar-start">
                            <Link className="navbar-item" to="/chat">{Localizer.term("Chat")}</Link>
                            <Link className="navbar-item is-active" to="/counter">{Localizer.term("Counter")}</Link>
                            <Link className="navbar-item" to="/fetch">{Localizer.term("Fetch")}</Link>
                        </div>
                    </div>
                    <div className="navbar-end">
                        <div className="navbar-item">
                            <div className="buttons">
                                <a className="button" onClick={this.openSwagger}>{Localizer.term("Swagger")}</a>
                                <Link className="button is-light" to="/language">Log in</Link>
                                <a className="button" onClick={this.toggleLangModal}>{Localizer.term("Language")}</a>
                                <Language modalState={this.state.langModal} closeModal={this.toggleLangModal} />
                            </div>
                        </div>
                    </div>
                </div>
            </nav>
        );
    }       
            
    render() {
        const contents = this.state.loading
            ? <progress className="progress is-small is-primary" max="100%">30%</progress>
            : this.renderNavbar();

        return (
            <header>
                {contents}
            </header>
        );
    }
}

