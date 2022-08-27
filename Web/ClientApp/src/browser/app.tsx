import * as React from 'react';

//import { Component } from 'react';
//import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
//import {
//    // useEffect,
//    useState, useEffect
//} from 'react';
//import React from 'react';
import { Routes, Route } from 'react-router';

import { Layout } from './view/layout';
import { Home } from './view/home';
import { Fetch } from './view/fetch';
import { Counter } from './view/counter';
import { Chat } from './view/chat';
//import { NotFound } from './view/not-found';

import { Language } from './dialogs/language';

//import { rootStore } from './model/store';

export default class App extends React.Component {
    render() {
        return (
            <Layout>
                <Routes>
                    <Route path='/' element={<Home readme={'home.md'} />} />
                    <Route path='/chat' element={<Chat />} />
                    <Route path='/counter' element={<Counter />} />
                    <Route path='/fetch' element={<Fetch />} />
                    <Route path='/language' element={<Language modalState={true} closeModal={""} />} />
                    <Route path='/swagger' />
                </Routes>
            </Layout>
        );
    }
}