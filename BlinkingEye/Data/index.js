(function() {
    /*setTimeout(function() {
        alert("xxx");
    }, 2000);*/

    var screen = document.getElementById('screen'),
        context = screen.getContext('2d'),
        ctr = 0,
        initial = true,
        failSafeTimerId,
        extend = function()  // From https://stackoverflow.com/questions/11197247/javascript-equivalent-of-jquerys-extend-method
        {
            for (var i = 1, ii = arguments.length; i < ii; i++)
                for (var key in arguments[i])
                    if (arguments[i].hasOwnProperty(key))
                        arguments[0][key] = arguments[i][key];
            return arguments[0];
        };

    (function () {
        var request = new XMLHttpRequest();
        request.open('GET', 'screen-size.json', true);
        request.onload = function() {
            if (request.status === 200)
                extend(screen, JSON.parse(request.responseText));
        };
        request.send();
    })();

    var loadImage = function(diff, callback) {
        diff = diff || false;
        var image = new Image();
        image.src = document.location + (diff ? 'screen-diff.png' : 'screen.png') + '?ctr=' + ctr;
        image.onload = function () {
            ctr++;
            clearTimeout(failSafeTimerId);
            context.drawImage(image, 0, 0);
            callback();
        };
    };

    var failSafeRestartLoading = function() {
        ctr++;
        initial = true;
        reloadImage();
    };

    var reloadImage = function() {
        var delay = 2000, // All delays in milliseconds
            failSafeDelay = 60000;

        if (initial) {
            loadImage(false, reloadImage);
            initial = false;
        } else {
            setTimeout(function() {
                loadImage(true, reloadImage);
            }, delay);
        }

        failSafeTimerId = setTimeout(failSafeRestartLoading, failSafeDelay);
    };

    reloadImage();

    // Input events
    var mouseIsDown = false,
        mouseMoveDelay = 2000, // ms
        mouseMoveTimerId = null,
        mouseMovePos = { x: -1, y: -1 },
        mouseMoveLastPos = extend({}, mouseMovePos);

    var sendEvent = function(type, otherParams) {
        console.log("Document's location is " + document.location);
        console.log("Event type is " + type);

        var params = { type: type };
        extend(params, otherParams);

        var qa = [];
        for (var key in params)
            qa.push(encodeURIComponent(key) + "=" + encodeURIComponent(params[key]));

        var request = new XMLHttpRequest();
        request.open('POST', document.location, true);
        request.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=UTF-8');
        request.onload = function() {
            // Callback
        };
        request.send(qa.join('&'));
    };

    var restartMouseMoveTimer = function() {
        if (mouseMoveTimerId === null) {
            mouseMoveTimerId = setTimeout(function() {
                mouseMoveTimerId = null;

                if (mouseMovePos.x !== mouseMoveLastPos.x && mouseMovePos.y !== mouseMoveLastPos.y) {
                    sendEvent("mousemove", mouseMovePos);
                    mouseMoveLastPos = extend({}, mouseMovePos);
                }
            }, mouseMoveDelay);
        } else {
            clearTimeout(mouseMoveTimerId);
            mouseMoveTimerId = null;
            restartMouseMoveTimer();
        }
    };

    /*
    var recognizeKeyPress = function(event) {  // This code comes from http://unixpapa.com/js/key.html
        var c = null;
      
        if (event.which == null)
            c = String.fromCharCode(event.keyCode); // old IE
        else if (event.which != 0 && event.charCode != 0)
            c = String.fromCharCode(event.which); // All others
        else
            ; // Special key
          
        return c;
    };
        
    var recognizeKeyDownAndUp = function(event) {
        var c = null;
          
        switch (event.keyCode) {
            case  32: c = 'space'; break;
            case  13: c = 'enter'; break;
            case   9: c = 'tab'; break;
            case  27: c = 'esc'; break;
            case   8: c = 'backspace'; break;
            case  16: c = 'shift'; break;
            case  17: c = 'control'; break;
            case  18: c = 'alt'; break;
            case  20: c = 'capslock'; break;
            case 144: c = 'numlock'; break;
            case  37: c = 'leftarrow'; break;
            case  38: c = 'uparrow'; break;
            case  39: c = 'rightarrow'; break;
            case  40: c = 'bottomarrow'; break;
            case  45: c = 'insert'; break;
            case  46: c = 'delete'; break;
            case  36: c = 'home'; break;
            case  35: c = 'end'; break;
            case  33: c = 'pageup'; break;
            case  34: c = 'pagedown'; break;
            case 112: c = 'f1'; break;
            case 113: c = 'f2'; break;
            case 114: c = 'f3'; break;
            case 115: c = 'f4'; break;
            case 116: c = 'f5'; break;
            case 117: c = 'f6'; break;
            case 118: c = 'f7'; break;
            case 119: c = 'f8'; break;
            case 120: c = 'f9'; break;
            case 121: c = 'f10'; break;
            case 122: c = 'f11'; break;
            case 123: c = 'f12'; break;
            // This key works on Mozilla only
            case 224: c = 'applecommand'; break;
            // And these keys also on IE
            case  91: c = 'leftwindowsstart'; break;
            case  92: c = 'rightwindowsstart'; break;
            case  93: c = 'windowsmenu'; break;
        }
          
        return c;
    };
        
    var recognizeSpecialKey = function(event) {
        if (recognizeKeyPress(event) === null) {
            var key = recognizeKeyDownAndUp(event);
            
            if (key !== null)
                return key;
        }

        return null;
    };
    */

    var mouseStateChangeParams = function(event) {
        var params = {
            which: event.which,
            altKey: event.altKey,
            controlKey: event.controlKey,
            metaKey: event.metaKey,
            shiftKey: event.shiftKey
        };

        extend(params, mouseMovePos);
        return params;
    };

    screen.onmousedown = function(event) {
        //    console.log("Mouse down; pageX: " + event.pageX + ", pageY: " + event.pageY);
        mouseIsDown = true;
        sendEvent("mousedown", mouseStateChangeParams(event));
        event.preventDefault();
        event.stopPropagation();
    };

    screen.onmousemove = function(event) {
        mouseMovePos = {
            'x': event.pageX,
            'y': event.pageY
        };

        if (mouseIsDown) {
          sendEvent("mousemove", mouseMovePos);
        } else {
            restartMouseMoveTimer();
        }

        event.preventDefault();
        event.stopPropagation();
    };

    screen.onmouseup = function(event) {
        //console.log("Mouse up; pageX: " + event.pageX + ", pageY: " + event.pageY);
        mouseIsDown = false;
        sendEvent("mouseup", mouseStateChangeParams(event));
        event.preventDefault();
        event.stopPropagation();
    };

    /*
    screen.onkeydown = function(event) {
        //console.log("Key down; pageX: " + event.pageX + ", pageY: " + event.pageY + ", metaKey: " + event.metaKey);
          
        var key = recognizeSpecialKey(event);
          
        if (key !== null)
            sendEvent("keydown");
    };
  
    screen.onkeyup = function(event) {
        //console.log("Key up; pageX: " + event.pageX + ", pageY: " + event.pageY + ", metaKey: " + event.metaKey);
          
        var key = recognizeSpecialKey(event);
          
        if (key !== null)
            sendEvent("keyup");
    };
  
    screen.onkeypress = function(event) {
        var key = recognizeKeyPress(event);
            
        if (key !== null)
            sendEvent("keypress");
    };
    */
})();
