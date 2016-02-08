$(document).ready(function()
{
  //setTimeout(function() { alert("xxx"); }, 2000);
  var screen = document.getElementById('screen'),
      context = screen.getContext('2d'),
      ctr = 0,
      initial = true,
      failSafeTimerId;
        
  $.get("screen-size.json", function(data) { $.extend(screen, data); });
            
  var loadImage = function(diff, callback)
  {
    diff = diff || false;
    var image = new Image();
    image.src = document.location + (diff ? 'screen-diff.png' : 'screen.png') + '?ctr=' + ctr;
    image.onload = function()
    {
      ctr++;
      clearTimeout(failSafeTimerId);
      context.drawImage(image, 0, 0);
      callback();
    };
  };
        
  var failSafeRestartLoading = function()
  {
    ctr++;
    initial = true;
    reloadImage();
  };
        
  var reloadImage = function()
  {
    var delay = 2000, // All delays in milliseconds
        failSafeDelay = 60000;

    if (initial)
    {
      loadImage(false, reloadImage);
      initial = false;
    }
    else
    {
      setTimeout(function() { loadImage(true, reloadImage); }, delay);
    }
          
    failSafeTimerId = setTimeout(failSafeRestartLoading, failSafeDelay);
  };
        
  reloadImage();
		
  // Input events
  var mouseIsDown = false,
      mouseMoveDelay = 2000, // ms
      mouseMoveTimerId = null,
      mouseMovePos = { x: -1, y: -1 },
      mouseMoveLastPos = $.extend({}, mouseMovePos);
       
  var sendEvent = function(type, otherParams)
  {
    console.log("Document's location is " + document.location);
    console.log("Event type is " + type);
    var params = { type: type };
    $.extend(params, otherParams);
    $.post(document.location,
           params,
           function(data) { /* Here could be a callback. */ });
  };
        
  var restartMouseMoveTimer = function()
  {
    if (mouseMoveTimerId === null)
    {
      mouseMoveTimerId = setTimeout(function()
      {
        mouseMoveTimerId = null;
              
        if (mouseMovePos.x !== mouseMoveLastPos.x && mouseMovePos.y !== mouseMoveLastPos.y)
        {
          sendEvent("mousemove", mouseMovePos);
          mouseMoveLastPos = $.extend({}, mouseMovePos);
        }
      }, mouseMoveDelay);
    }
    else
    {
      clearTimeout(mouseMoveTimerId);
      mouseMoveTimerId = null;
      restartMouseMoveTimer();
    }
  };
        
  /*
  var recognizeKeyPress = function(event) // This code comes from http://unixpapa.com/js/key.html
  {
    var c = null;
      
    if (event.which == null)
      c = String.fromCharCode(event.keyCode); // old IE
    else if (event.which != 0 && event.charCode != 0)
      c = String.fromCharCode(event.which); // All others
    else
      ; // Special key
          
    return c;
  };
        
  var recognizeKeyDownAndUp = function(event)
  {
    var c = null;
          
    switch (event.keyCode)
    {
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
        
  var recognizeSpecialKey = function(event)
  {
    if (recognizeKeyPress(event) === null)
    {
      var key = recognizeKeyDownAndUp(event);
            
      if (key !== null)
        return key;
    }

    return null;
  };
  */
  
  $(screen).mousedown(function(event)
  {
//    console.log("Mouse down; pageX: " + event.pageX + ", pageY: " + event.pageY);
    mouseIsDown = true;
    var params =
    {
      which: event.which,
      altKey: event.altKey,
      controlKey: event.controlKey,
      metaKey: event.metaKey,
      shiftKey: event.shiftKey
    };
    $.extend(params, mouseMovePos);
    sendEvent("mousedown", params);
    event.preventDefault();
    event.stopPropagation();
  }).mousemove(function(event)
  {
    mouseMovePos = { 'x': event.pageX, 'y': event.pageY };
          
    if (mouseIsDown)
    {
      sendEvent("mousemove", mouseMovePos);
    }
    else
    {
      // restartMouseMoveTimer();
    }
    event.preventDefault();
    event.stopPropagation();
  }).mouseup(function(event)
  {
//    console.log("Mouse up; pageX: " + event.pageX + ", pageY: " + event.pageY);
    mouseIsDown = false;
    var params =
    {
      which: event.which,
      altKey: event.altKey,
      controlKey: event.controlKey,
      metaKey: event.metaKey,
      shiftKey: event.shiftKey
    };
    $.extend(params, mouseMovePos);
    sendEvent("mouseup", params);
    event.preventDefault();
    event.stopPropagation();
  });
        
  /*
  $(screen).keydown(function(event)
  {
//    console.log("Key down; pageX: " + event.pageX + ", pageY: " + event.pageY + ", metaKey: " + event.metaKey);
          
    var key = recognizeSpecialKey(event);
          
    if (key !== null)
      sendEvent("keydown");
  }).keyup(function(event)
  {
//    console.log("Key up; pageX: " + event.pageX + ", pageY: " + event.pageY + ", metaKey: " + event.metaKey);
          
    var key = recognizeSpecialKey(event);
          
    if (key !== null)
      sendEvent("keyup");
  }).keypress(function(event)
  {
    var key = recognizeKeyPress(event);
            
    if (key !== null)
      sendEvent("keypress");
  });
  */
});
