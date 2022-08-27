//import { Component, useState, useEffect } from 'react';
import { Component } from 'react';
import * as signalR from '@microsoft/signalr';
import { Localizer } from '../model/localizer';
//import { useObservable } from "../model/observation";
//import { useFetch } from "../model/hooks";
//import { rootStore } from "../model/store";

export class Chat extends Component<{}> {
    //displayName = Chat.name
    connection: signalR.HubConnection | null;

    constructor(props: {}) {
        super(props);

        // create the connection instance
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/chat")
            .build();
    }

    componentDidMount() {

        if (this.connection !== null) {

            // add send handle
            this.connection.on('Send', (nick, message) => {
                this.appendLine(nick, message);
            });

            // attempt connection, and handle errors
            this.connection.start()
                .then(() => console.info('SignalR Connected'))
                .catch(e => console.error('SignalR Connection Error: ', e));
        }

        // hide chat area
        const chat = document.getElementById('chat');
        if (chat !== null) {
            chat.style.visibility = "hidden";
        }
    }

    componentWillUnmount() {
        if (this.connection !== null) {
            this.connection.stop();
            this.connection = null;
        }
    }

    appendLine(nick: string, message: string) {
        const nameElement = document.createElement('strong');
        nameElement.innerText = `${nick}:`;

        const msgElement = document.createElement('em');
        msgElement.innerText = ` ${message}`;

        const li = document.createElement('li');
        li.appendChild(nameElement);
        li.appendChild(msgElement);

        const messages = document.getElementById('messages') as HTMLUListElement;
        messages.append(li);
    }

    onSubmit() {
        const message = document.getElementById('message');
        if (message instanceof HTMLInputElement) {
            const text = message.value;
            const nick = document.getElementById('spn-nick');
            if (nick instanceof HTMLElement) {

                message.value = '';

                if (this.connection !== null) {
                    this.connection.invoke('Send', nick.innerText, text);
                }
            }
        }
    }

    continueToChat() {
        const snick = document.getElementById('spn-nick');
        if (snick instanceof HTMLElement) {
            const nick = document.getElementById('nick');
            if (nick instanceof HTMLInputElement) {
                snick.innerHTML = nick.value;
                const entrance = document.getElementById('entrance');
                if (entrance !== null) {
                    entrance.style.visibility = "hidden";
                }
                const chat = document.getElementById('chat');
                if (chat !== null) {
                    chat.style.visibility = "visible";
                }
            }
        }
    }

    render() {
        return (
            <div>
                <h1 className="title">{Localizer.term("Chat")}</h1>

                <div className="notification">
                    &nbsp;
                </div>

                <div id="entrance" className="field">
                    <label className="label" htmlFor="nick">Enter your nickname:</label>
                    <input className="input" type="text" id="nick" />
                    <button className="button is-link" onClick={this.continueToChat}>Continue</button>
                </div>

                <div id="chat">
                    <h3 id="spn-nick">&nbsp;</h3>
                    <div id="send-message" className="field">
                        <label className="label" htmlFor="message">Message:</label>
                        <input className="input" type="text" id="message" />
                        <button id="send" className="button" onClick={() => this.onSubmit()}>Send</button>
                    </div>
                    <div className="clear" />
                    <ul id="messages" />
                </div>

                <p><em>SignalR</em> demo.</p>
            </div>
        );
    }
}