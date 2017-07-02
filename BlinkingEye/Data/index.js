(function() {
    // Screen handling part
    var screen = document.getElementById('screen'),
        context = screen.getContext('2d'),
        ctr = 0,
        initial = true,
        failSafeTimerId,
        extend = function() {  // From https://stackoverflow.com/questions/11197247/javascript-equivalent-of-jquerys-extend-method
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
            initial = false;
            context.drawImage(image, 0, 0);
            callback();
        };
        image.onerror = failSafeRestartLoading;
    };

    var failSafeRestartLoading = function() {
        ctr++;
        initial = true;
        reloadImage();
    };

    var reloadImage = function() {
        var delay = 2000, // All delays in milliseconds
            failSafeDelay = 60000;

        if (initial)
            loadImage(false, reloadImage);
        else {
            setTimeout(function() {
                loadImage(true, reloadImage);
            }, delay);
        }

        if (failSafeTimerId) {
            clearTimeout(failSafeTimerId);
            failSafeTimerId = undefined;
        }
        failSafeTimerId = setTimeout(failSafeRestartLoading, failSafeDelay);
    };

    reloadImage();

    // (Input-)Event handling part
    var eventQueue = [],
        eventRequest,
        eventTimer;

    var serializeObjectAsURLEncoded = function(obj) {
        var pairs = [];

        for (var key in obj)
            pairs.push(encodeURIComponent(key) + '=' + encodeURIComponent(obj[key]));

        return pairs.join('&');
    };

    var sendEvents = function() {
        var event = eventQueue.shift();

        console.log('event to send:', JSON.stringify(event));

        eventRequest = new XMLHttpRequest();
        eventRequest.open('POST', document.location, true);
        eventRequest.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=UTF-8');
        eventRequest.onload = function() {
            eventRequest = undefined;
            if (eventQueue.length > 0)
                sendEvents();
        };
        eventRequest.send(serializeObjectAsURLEncoded(event));
    };

    (function() {
        eventTimer = setInterval(function() {
            if (eventQueue.length > 0 && !eventRequest)
                sendEvents();
        }, 100);
    })();

    // Mouse events
    var mouseIsDown = false,
        mouseMoveDelay = 2000, // ms
        mouseMoveTimerId,
        mouseMovePos = { x: -1, y: -1 },
        mouseMoveLastPos = extend({}, mouseMovePos);
    
    var updateMouseMovePos = function(event) {
        mouseMovePos = {
            x: 'pageX' in event ? event.pageX : event.clientX + document.documentElement.scrollLeft,
            y: 'pageY' in event ? event.pageY : event.clientY + document.documentElement.scrollTop
        };
    };

    var restartMouseMoveTimer = function() {
            if (!mouseMoveTimerId) {
                mouseMoveTimerId = setTimeout(function() {
                    mouseMoveTimerId = undefined;
                    if (mouseMovePos.x !== mouseMoveLastPos.x || mouseMovePos.y !== mouseMoveLastPos.y) {
                        eventQueue.push(extend({type: 'mousemove'}, mouseMovePos));
                        mouseMoveLastPos = extend({}, mouseMovePos);
                    }
                }, mouseMoveDelay);
            } else {
                clearTimeout(mouseMoveTimerId);
                mouseMoveTimerId = undefined;
                restartMouseMoveTimer();
            }
        };

    // Install event handlers
    var createMouseMoveEventParams = function(event) {
        updateMouseMovePos(event);
        return extend({type: 'mousemove'}, mouseMovePos);
    };

    var commonEventHandler = function(event) {
        var params = {
            type: event.type
        };
        
        switch (params.type) {
            case 'mousedown':
                eventQueue.push(createMouseMoveEventParams(event));
                mouseIsDown = true;
                params.which = event.which;
                eventQueue.push(params);
                break;
            case 'mouseup':
                eventQueue.push(createMouseMoveEventParams(event));
                mouseIsDown = false;
                params.which = event.which;
                eventQueue.push(params);
                break;
            case 'mousemove':
                updateMouseMovePos(event);
                restartMouseMoveTimer();
                break;
            case 'wheel':
                if ('deltaX' in event && event.deltaX !== 0)
                    params.deltaX = event.deltaX;
                if ('deltaY' in event && event.deltaY !== 0)
                    params.deltaY = event.deltaY;
                if ('deltaX' in params || 'deltaY' in params)
                    eventQueue.push(params);
                break;
            case 'keydown':
            case 'keyup':
                params.key = event.key;
                if ('keyCode' in event)
                    params.keyCode = event.keyCode;
                else if ('which' in event)
                    params.keyCode = event.which;
                else
                    console.log('This browser uses neither keyCode nor which');
                eventQueue.push(params);
                break;
            case 'keypress':  // Ignore them
                break;
            default:
                console.log('Unknown event of type', event.type);
                break;
        }

        event.stopPropagation();
        event.preventDefault();
        return false;  // This is both preventDefault() and stopPropagation()
    };

    (function installEventHandlers() {
        var installHandler = function(element, event, handler) {
            if (element.addEventListener)
                element.addEventListener(event, handler, false);
            else if (element.attachEvent)
                element.attachEvent('on' + event, handler);
            else
                console.log('This browser uses neither addEventListener nor attachEvent');
        };

        installHandler(screen, 'mousedown', commonEventHandler);
        installHandler(screen, 'mousemove', commonEventHandler);
        installHandler(screen, 'mouseup', commonEventHandler);
        installHandler(window, 'wheel', commonEventHandler);
        installHandler(window, 'keydown', commonEventHandler);
        installHandler(window, 'keypress', commonEventHandler);
        installHandler(window, 'keyup', commonEventHandler);
    })();
})();
