import * as React from 'react';
import logo from '../images/uniya.png'
import { Navigate } from './navigate';

export class Layout extends React.Component {

    render() {
        return (
            <div>
                <Navigate logoAlt="Unia system" logoUrl={logo} />
                <div className="container">
                    {this.props.children}
                </div>
            </div>
        );
    }
}
