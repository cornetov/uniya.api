import * as React from 'react';
import { Localizer } from '../model/localizer';


export class NotFound extends React.Component {

    render() {
        return (
            <div>
                <h1 className="title">{Localizer.term("NotFound")}</h1>

                <p>This page not found.</p>
            </div>
        );
    }
}