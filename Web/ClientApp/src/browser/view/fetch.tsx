//import React from 'react';
import * as React from 'react';
//import { useFetch } from "../model/hooks";
import { Localizer } from '../model/localizer';

export interface FetchProps {
}
export interface FetchState {
    loading: boolean;
    forecasts: any[] | null;
}

export class Fetch extends React.Component<FetchProps, FetchState> {

    constructor(props: FetchProps) {
        super(props);
        this.state = { loading: true, forecasts: null };
    }

    async setStateAsync(state: FetchState) {
        return new Promise<void>((render) => {
            this.setState(state, render);
        });
    }

    async componentDidMount() {

        // load localized terms
        try {

            //const r = useFetch('api/Culture/WeatherForecasts');
            const r = await fetch('weatherforecast');
            if (r.ok) {
            //    throw Error(r.statusText);
                const json = await r.json();
                await this.setStateAsync({ loading: false, forecasts: json });
            }
            //const json = await r.json();

            //if (r.response) {
                // wait context
            //    return (
            //        <div>
            //            <progress className="progress is-small is-primary" max="100%">30%</progress>
            //        </div>
            //    );
            //    await this.setStateAsync({ loading: false, forecasts: r.response as any[] });
            //}
            //await this.setStateAsync({ loading: true, forecasts: json as any[] });
        } catch (error) {
            console.log(error);
        }
    }

    render() {
        const colWidth = 400;
        const forecasts = this.state.forecasts as any[];

        const contents = this.state.loading
            ? <progress className="progress is-small is-primary" max="100%">30%</progress>
            :
            <div>
                <h1 className="title">Weather forecast</h1>
                <p>This component demonstrates fetching data from the server.</p>
                <div className="table-container">
                    <table className="table is-narrow is-striped is-bordered is-hoverable">
                        <thead>
                            <tr>
                                <th><label className="checkbox"><input type="checkbox"></input></label></th>
                                <th>
                                    <div className="box" style={{ width: colWidth + "px" }}>
                                        <span>{Localizer.term("Date")}</span>
                                        <button className="button is-white is-small is-right">
                                            <span className="icon is-small">
                                                <i className="fas fa-filter"></i>
                                            </span>
                                        </button>
                                    </div>
                                </th>
                                <th>
                                    <span>Temp. (C)</span>
                                    <button className="button is-white is-small">
                                        <span className="icon is-small is-right">
                                            <i className="fas fa-filter"></i>
                                        </span>
                                    </button>
                                </th>
                                <th>
                                    <span>Temp. (F)</span>
                                    <button className="button is-white is-small">
                                        <span className="icon is-small is-right">
                                            <i className="fas fa-filter"></i>
                                        </span>
                                    </button>
                                </th>
                                <th>
                                    <span>{Localizer.term("Summary")}</span>
                                    <button className="button is-white is-small">
                                        <span className="icon is-small is-right">
                                            <i className="fas fa-filter"></i>
                                        </span>
                                    </button>
                                </th>
                            </tr>
                        </thead>
                        <tbody>
                            {forecasts.map(forecast =>
                                <tr key={forecast['dateFormatted']}>
                                    <td><label className="checkbox"><input type="checkbox"></input></label></td>
                                    <td className="has-text-centered">{forecast['date']}</td>
                                    <td className="has-text-right">{forecast['temperatureC']}</td>
                                    <td className="has-text-right">{forecast['temperatureF']}</td>
                                    <td>{forecast['summary']}</td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>;

        return (
            <div>
                {contents}
            </div>
        );
    }
}