// renderers.ts
import { showElementById } from "./helpers.js";
import { MatchStatus } from "./models.js";
/**
    * Renders a tic-tac-toe board.
    * @param board The board data.
    */
export function renderMatchBoard(board) {
    const boardSymbolMap = { "32": " ", "88": "X", "79": "O" };
    const empty = ["", "", ""];
    if (!board || board.every(row => row === null))
        board = [empty, empty, empty];
    return `<table class="match-board">${board.map(row => `
        <tr>${row.map(cell => `<td>${cell === "Empty" ? "" : boardSymbolMap[cell] ?? cell ?? " "}</td>`).join('')}</tr>`).join('')}
    </table>`;
}
/**
* Renders a list of matches.
* @param matches The matches array.
*/
export function renderMatches(matches) {
    const container = document.getElementById("matchesContainer");
    if (!container)
        return;
    container.innerHTML = '';
    if (matches && matches.length > 0) {
        showElementById('matchesPanel');
    }
    for (let idx = 0; idx < matches.length; idx++) {
        renderMatch(matches[idx], idx);
    }
    reorderMatches();
}
/**
* Renders or Updates a specific match
* @param match Match.
* @param matchIndex Index.
*/
export function renderMatch(match, matchIndex) {
    const container = document.getElementById("matchesContainer");
    if (!container)
        return;
    let matchDiv = container.querySelector(`[data-match-id="${match.id}"]`);
    let cardColor = "bg-light";
    const status = MatchStatus[Number(match.status)];
    if (status === "Ongoing")
        cardColor = "bg-success text-white";
    else if (status === "Cancelled")
        cardColor = "bg-danger text-white";
    else if (status === "Finished")
        cardColor = "bg-secondary text-white";
    else if (status === "Planned")
        cardColor = "bg-info text-dark";
    const matchHtml = `
        <div class="card col-3 m-2 ${cardColor} flash-change" data-match-id="${match.id}" data-status="${status}">
            <strong>Match ${matchIndex !== undefined ? matchIndex + 1 : ''}:</strong> ${match.playerAName} (X) vs ${match.playerBName} (O)<br>
            <strong>Status:</strong> ${status}<br>
            <strong>Duration:</strong> ${match.duration || '-'}
            ${renderMatchBoard(match.board)}
        </div>
    `;
    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = matchHtml;
    const newMatchDiv = tempDiv.firstElementChild;
    if (matchDiv) {
        container.replaceChild(newMatchDiv, matchDiv);
    }
    else {
        container.appendChild(newMatchDiv);
    }
}
const priorityOrder = ['Ongoing', 'Planned', 'Finished', 'Cancelled'];
function reorderMatches() {
    const container = document.getElementById('matchesContainer');
    if (!container) {
        return;
    }
    const cards = Array.from(container.children);
    cards.sort((a, b) => {
        const statusA = a.attributes.getNamedItem("data-status")?.value || '';
        const statusB = b.attributes.getNamedItem("data-status")?.value || '';
        return priorityOrder.indexOf(statusA) - priorityOrder.indexOf(statusB);
    });
    for (const card of cards) {
        container.appendChild(card);
    }
}
export function updateMatchBoard(matchId, board) {
    const matchDiv = document.querySelector(`[data-match-id="${matchId}"]`);
    if (!matchDiv) {
        console.error(`Match with id ${matchId} not found.`);
        return;
    }
    const renderedBoard = renderMatchBoard(board);
    const table = matchDiv.querySelector("table");
    if (table) {
        // Criar um container temporário para renderizar o novo board
        const tempContainer = document.createElement("div");
        tempContainer.innerHTML = renderedBoard.trim();
        const newTable = tempContainer.querySelector("table");
        if (newTable) {
            table.replaceWith(newTable);
        }
    }
    else {
        console.error(`No table found inside match card ${matchId}.`);
    }
}
/**
    * Renders the tournament leaderboard.
    * @param players The leaderboard array.
    */
export function renderLeaderboard(players) {
    const container = document.getElementById("leaderboardContainer");
    if (!container)
        return;
    const table = `<table class="table table-striped table-hover">
        <thead><tr><th>Position</th><th>Player</th><th>Score</th></tr></thead>
        <tbody>
            ${players.map((p, i) => {
        let medal = i === 0 ? '<i class="fas fa-medal" style="color:gold"></i>' :
            i === 1 ? '<i class="fas fa-medal" style="color:silver"></i>' :
                i === 2 ? '<i class="fas fa-medal" style="color:#cd7f32"></i>' :
                    (i + 1).toString();
        let rowClass = i === 0 ? "table-warning fw-bold" : "";
        return `<tr class="${rowClass}"><td>${medal}</td><td>${p.name}</td><td>${p.score}</td></tr>`;
    }).join('')}

        </tbody>
    </table>`;
    container.innerHTML = table;
}
/**
    * Renders the list of players.
    * @param players The players array.
    */
export function renderPlayers(players) {
    const container = document.getElementById("playersContainer");
    if (!container)
        return;
    container.innerHTML = `
        <ul class='small'>` + players.map(p => `
        <li id="player-${p.id}">
            ${p.name}
            <button class="btn btn-sm btn-outline-secondary ms-2 renamePlayerBtn" data-player-id="${p.id}">
                <i class="fas fa-edit"></i>
            </button>
        </li>
    `).join('') + "</ul>";
}
