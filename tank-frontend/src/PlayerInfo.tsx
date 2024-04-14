import {useEffect, useState} from 'react';
import {Player, getPlayer} from './serverCalls';
import {Guid} from "./Guid.ts";
import Column from "./components/Column.tsx";

export default function PlayerInfo({playerId, logout}: {
    playerId: Guid,
    logout: () => void
}) {
    const [player, setPlayer] = useState<Player | null>();

    useEffect(() => {
        const refresh = () => {
            getPlayer(playerId).then(response => {
                if (response.successResult)
                    setPlayer(response.successResult);
                else
                    logout();
            });
        };

        const timer = setInterval(refresh, 5000);
        return () => clearInterval(timer);
    }, [playerId]);

    return <Column className='PlayerInfo flex-grow'>
        <h3>
            {player ? `Playing as ${player?.name}` : 'loading...'}
        </h3>
        <table>
            <tbody>
            <tr>
                <td>kills:</td>
                <td>{player?.scores.kills}</td>
            </tr>
            <tr>
                <td>deaths:</td>
                <td>{player?.scores.deaths}</td>
            </tr>
            </tbody>
        </table>
    </Column>;
}
