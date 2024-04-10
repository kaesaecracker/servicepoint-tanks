import {useEffect, useState} from 'react';
import './JoinForm.css';

type PlayerResponse = {
    readonly name: string;
    readonly id: string;
};

export async function fetchPlayer(name: string, options: RequestInit) {
    const url = new URL(import.meta.env.VITE_TANK_PLAYER_URL);
    url.searchParams.set('name', name);

    const response = await fetch(url, options);
    if (!response.ok)
        return null;

    const json = await response.json() as PlayerResponse;
    return json.id;
}

export default function JoinForm({onDone}: { onDone: (id: string) => void }) {
    const [name, setName] = useState('');
    const [clicked, setClicked] = useState(false);
    const [data, setData] = useState<PlayerResponse | null>(null);

    useEffect(() => {
        if (!clicked || data)
            return;

        try {
            fetchPlayer(name, {}).then((value: string | null) => {
                if (value)
                    onDone(value);
                else
                    setClicked(false);
            });
        } catch (e) {
            console.log(e);
            alert(e);
        }
    }, [clicked, setData, data]);

    const disableButtons = clicked || name.trim() === '';
    return <div className="JoinForm">
        <input
            type="text"
            value={name}
            onChange={e => setName(e.target.value)}
        />
        <button
            onClick={() => setClicked(true)}
            disabled={disableButtons}
        >
            join
        </button>
    </div>;
}