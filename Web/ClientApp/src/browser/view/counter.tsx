import * as React from 'react';
//import { Localizer } from '../model/localizer';
import { rootStore } from "../model/store";

export function Counter() {

    const [currentCount, setCurrentCount] = React.useState<number>(0);
    const onIncrementCounter = React.useCallback(() => {
        setCurrentCount(currentCount + 1);
    }, [currentCount]);

    return (
        <div>
            <h1 className="title">{rootStore.getTerm("Counter")}</h1>
            <p>This is a simple example of a React component.</p>
            <p>Current count: <strong>{currentCount}</strong></p>
            <button className="button is-primary" onClick={onIncrementCounter}>Increment {rootStore.getTerm("Counter")}</button>
        </div>
    )
}