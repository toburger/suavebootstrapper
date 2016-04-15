/// <reference path="ts/google.visualization.d.ts" />
/// <reference path="ts/es6-promise.d.ts" />
/// <reference path="ts/collections.d.ts" />

interface Program {
    titel: string
    untertitel: string
    beginn: string
    ende: string
    beschreibung: string
    url: string
}

interface ChannelProgram {
    [index: string]: Program[];
}

interface ChannelLive {
    [index: string]: Program;
}

function getJson<T>(url, method = "GET"): Promise<T> {
    return new Promise<T>((resolve, reject) => {
        var r = new XMLHttpRequest();
        r.open(method, url, true);
        r.onreadystatechange = function () {
            if (r.readyState != 4 || r.status != 200) return;
            else resolve(JSON.parse(r.responseText));
        };
        r.onerror = data => reject(data.error);
        r.send();
    });
}

function parseProgram(channelName: string, program: Program): any[] {
    var name = program.titel;
    var beginn = new Date(Date.parse(program.beginn));
    var ende = new Date(Date.parse(program.ende));
    return [channelName, name, beginn, ende];
}

function getNowPlaying(channelLive: ChannelLive): any[][] {
    var programs = [];
    for (var channelName in channelLive) {
        var prog = channelLive[channelName];
        programs.push(parseProgram(channelName, prog));
    }
    return programs;
}

function getProgram(channelProgram: ChannelProgram): [number, any[][]] {
    var programs = [];
    var numberOfRows = 0;
    for (var channelName in channelProgram) {
        var progs = channelProgram[channelName];
        programs = programs.concat(
            progs.map(prog => parseProgram(channelName, prog))
        );
        numberOfRows++;
    }
    return [numberOfRows, programs];
}

google.setOnLoadCallback(drawChart);

function getOptions(numberOfRows: number): google.visualization.TimelineOptions {
    var paddingHeight = 40;
    var rowHeight = numberOfRows * 41;
    var chartHeight = rowHeight + numberOfRows;

    var options = {
        'title': 'Movietimes',
        //'colors': ['#cbb69d', '#603913', '#c69c6e'],
        'avoidOverlappingGridLines': false,
        'timeline': {
            //'showRowLabels': true,
            'colorByRowLabel': true,
            //'singleColor': '#8d8'
        },
        'width': 4800,
        'height': chartHeight,
    };

    return options;
}

function scrollToTime(chartDiv): void {
    // TODO: scroll the chart to the current time
    //chartDiv.scrollLeft = 2000;
}

function drawChart() {
    var dataTable = new google.visualization.DataTable()

    dataTable.addColumn('string', 'Channel');
    dataTable.addColumn('string', 'Name');
    dataTable.addColumn('date', 'Start');
    dataTable.addColumn('date', 'End');

    var chartDiv = document.getElementById('chart_div');
    var chart = new google.visualization.Timeline(chartDiv);

    //var getData = getJson("live").then(getNowPlaying);
    var getData = getJson("program").then(getProgram);

    getData.then(tpl => {
        var numberOfRows = tpl[0];
        var program = tpl[1];
        dataTable.addRows(program);
        var options = getOptions(numberOfRows);
        chart.draw(dataTable, options);
        scrollToTime(chartDiv);
    });
}