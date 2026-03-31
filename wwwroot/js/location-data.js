const CHANDIGARH_SECTORS = [
    "Sector 1", "Sector 2", "Sector 3", "Sector 4", "Sector 5", "Sector 6", "Sector 7", "Sector 8", "Sector 9", "Sector 10",
    "Sector 11", "Sector 12", "Sector 13 (Manimajra)", "Sector 14", "Sector 15", "Sector 16", "Sector 17", "Sector 18", "Sector 19", "Sector 20",
    "Sector 21", "Sector 22", "Sector 23", "Sector 24", "Sector 25", "Sector 26", "Sector 27", "Sector 28", "Sector 29", "Sector 30",
    "Sector 31", "Sector 32", "Sector 33", "Sector 34", "Sector 35", "Sector 36", "Sector 37", "Sector 38", "Sector 38 West", "Sector 39", "Sector 40",
    "Sector 41", "Sector 42", "Sector 43", "Sector 44", "Sector 45", "Sector 46", "Sector 47", "Sector 48", "Sector 49", "Sector 50",
    "Sector 51", "Sector 52", "Sector 53", "Sector 54", "Sector 55", "Sector 56", "Sector 61", "Sector 63",
    "Industrial Area Phase 1", "Industrial Area Phase 2",
    "Manimajra", "Burail", "Kajheri", "Palsora", "Maloya", "Dadumajra", "Dhanas", "Khuda Lahora", "Khuda Jassu", "Khuda Ali Sher",
    "Kishangarh", "Kaimbwala", "Sarangpur", "Behlana", "Raipur Khurd", "Raipur Kalan", "Makhan Majra", "Daria", "Mauli Jagran", "Vikas Nagar"
];

function populateChdSectors(selectId) {
    const $select = $(`#${selectId}`);
    if ($select.length) {
        $select.empty().append(new Option('Select Sector / Area', ''));
        CHANDIGARH_SECTORS.forEach(s => {
            $select.append(new Option(s, s));
        });
    }
}
