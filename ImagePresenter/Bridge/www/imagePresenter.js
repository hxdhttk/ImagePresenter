/**
 * @version 1.0.0.0
 * @compiler Bridge.NET 15.6.0
 */
Bridge.assembly("ImagePresenter", function ($asm, globals) {
    "use strict";

    Bridge.define("ImagePresenter.App", {
        $main: function () {
            var img = Bridge.merge(new Image(), {
                id: "bg",
                width: window.innerWidth,
                height: window.innerHeight,
                src: "http://127.0.0.1:13017/"
            } );

            document.body.appendChild(img);
        }
    });
});
