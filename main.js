/*  Galileo Connections
*   LCD RGB - I2C (near D7)
*   
*   
*/


var mraa = require ('mraa');
// Load lcd module on I2C
var LCD = require('jsupm_i2clcd');
var async = require('async');

var express = require('express');
var app = express();
var bodyParser = require('body-parser');
var Rx = require('rx');

var redLed = new mraa.Gpio(2);
redLed.dir(mraa.DIR_OUT);
var redLedState = true;

app.use(bodyParser.json());

// Initialize Jhd1313m1 at 0x62 (RGB_ADDRESS) and 0x3E (LCD_ADDRESS) 
var lcdScreen = new LCD.Jhd1313m1 (0, 0x3E, 0x62);

var queue = {
    one: [],
    two: []
};
var lcdLetterShowTimeMS = 350;
var lcdRowLength = 16;
var lines = {
    one: {
        continueWithMessage: false,
        timePassed: false
    },
    two: {
        continueWithMessage: false,
        timePassed: false
    }
};

Number.prototype.map = function( in_min , in_max , out_min , out_max ) {
  return ( this - in_min ) * ( out_max - out_min ) / ( in_max - in_min ) + out_min;
}

String.prototype.repeat = function( num ) {
    return new Array( num + 1 ).join( this );
}

var lcdRowLength = 16;

rotateLcdText = function(text) {
    return text.substring(1, text.length) + text[0];
}

moreSpaces = function(text) {
    var spacesToAdd = Math.ceil(text.length/lcdRowLength)*lcdRowLength - text.length;
    return text.concat(" ".repeat(spacesToAdd));
}

setLcdTextLine1 = function(text) { 
    clearLcdLine(0);
    text = moreSpaces(text);
    var arrFunctions = [];
    
    lcdScreen.setCursor(0, 0);
    lcdScreen.write(text);
    
    if (text.length > 16) {
        for (index = 0; index < text.length; index++) {
            arrFunctions[index] = function(callback) {
                lcdScreen.setCursor(0, 0);
                lcdScreen.write(text);
                text = rotateLcdText(text);
                setTimeout(function(){ callback(null); }, lcdLetterShowTimeMS);
            };
        }
        
        async.series(arrFunctions, function() {
            //if (lines.one.continueWithMessage || !lines.one.timePassed) { setLcdTextLine1(text); }
            if (!lines.one.timePassed) {
                setLcdTextLine1(text);
            } else {
                if (queue.one.length != 0) {
                    setLcdTextLine1(queue.one.shift().text);
                }
            }
        });
    }
}

setLcdTextLine2 = function(text) { 
    clearLcdLine(1);
    text = moreSpaces(text);
    var arrFunctions = [];
    
    lcdScreen.setCursor(1, 0);
    lcdScreen.write(text);
    
    if (text.length > 16) {
        for (index = 0; index < text.length; index++) {
            arrFunctions[index] = function(callback) {
                lcdScreen.setCursor(1, 0);
                lcdScreen.write(text);
                text = rotateLcdText(text);
                setTimeout(function(){ callback(null); }, lcdLetterShowTimeMS);
            };
        }
        
        async.series(arrFunctions, function() {
            //if (lines.two.continueWithMessage || !lines.two.timePassed) { setLcdTextLine2(text); }
            if (!lines.two.timePassed) {
                setLcdTextLine2(text);
            } else {
                if (queue.two.length != 0) {
                    setLcdTextLine2(queue.two.shift().text);
                }
            }
        });
    }
}

setLcdColor = function(red,green,blue) {
    lcdScreen.setColor(red,green,blue);
}

clearLcdLine = function(line) {
    lcdScreen.setCursor(line, 0);
    lcdScreen.write(moreSpaces(" "));
}

app.post('/lcd/text', function(req,res) {
    
    var line = req.body.line;
    var text = req.body.text;
    var timeout = req.body.timeout;
    
    if (line == 1) {
        
        queue.one.push(req.body);
        runQueue1();
        /*if (queue.one.length == 0) {
            setLcdTextLine1(text);
            if (timeout >= 0) {
                setTimeout(function() { lines.one.timePassed = true; }, timeout || (lcdLetterShowTimeMS * text.length));
            }
        } else {
            
        }*/
        
        /*if (lines.one.continueWithMessage || !lines.one.timePassed) {
            
        }*/
        res.send("Coolz!");
    }
    else if (line == 2) {
        queue.two.push(req.body);
        runQueue2();
//        setLcdTextLine2(text);
//        setTimeout(function() { lines.one.timePassed = true; }, lcdLetterShowTimeMS * text.length);
        res.send("Coolz!");
    } else {
        res.send("Nope :(");
    }
});

var runQueue1 = function(){
    var runWrite = function(text, timeout, callback){
        setLcdTextLine1(text);
            if (timeout >= 0) {
                setTimeout(function() { lines.one.timePassed = true; callback(); }, timeout || (lcdLetterShowTimeMS * text.length));
            } else {
                callback();
            }
    };
    var arr = [];
    for(var i=0;i<queue.one.length;i++) {
        var temp = queue.one.slice(i,i+1)[0];
        arr.push(runWrite(temp.text, temp.timeout, function() {
            queue.one.shift();
        }));
    }
    async.series(arr, function() {
        //runQueue();
    });
};

var runQueue2 = function(){
    var runWrite = function(text, timeout, callback){
        setLcdTextLine2(text);
            if (timeout >= 0) {
                setTimeout(function() { lines.two.timePassed = true; callback(); }, timeout || (lcdLetterShowTimeMS * text.length));
            } else {
                callback();
            }
    };
    var arr = [];
    for(var i=0;i<queue.two.length;i++) {
        var temp = queue.two.slice(i,i+1)[0];
        arr.push(runWrite(temp.text, temp.timeout, function() {
            queue.two.shift();
        }));
    }
    async.series(arr, function() {
        //runQueue();
    });
};


lcdScreen.clear();
setLcdColor(200, 30, 50);
queue.one.push({line:0, text: "Joe's Tamagotchi", timeout: -1});
runQueue1();
queue.two.push({line:0, text: "I'm hungry, feed me!", timeout: -1});
runQueue2();

//setLcdTextLine2("Hello World!%#^tamtimtom");



/*
setTimeout(function() {
    continueWithMessage = false;
}, 6000);
*/

function blinkRed(on,off)
{
    if (redLedState) {
        
    }
    redLed.write(redLedState?1:0);
    redLedState = !redLedState;
    setTimeout(blinkRed,200);
}

blinkRed();

app.listen(3000);