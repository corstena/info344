$(document).ready(function () {
    ///Function to do AJAX search.
    function search() {
        var searchQuery = $('input#searchBox').val();
        searchQuery = searchQuery.toLowerCase();
        $('b#searchPrefix').text(searchQuery);
        searchQuery = searchQuery.replace(/ /g, "_");
        console.log(searchQuery);
        $.ajax({
            type: "POST",
            url: "/getQuerySuggestions.asmx/searchTrie",
            data: JSON.stringify({ prefix: searchQuery }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            cache: false,
            success: function (result) {
                var resultList = JSON.parse(result.d);
                var listHTML = "";
                for (var i = 0; i < resultList.length; i++) {
                    var html = "";
                    html += "<ul><li class=\"result\">";
                    html += "<h3>nameString</h3>";
                    html += "</li>";
                    html += "</ul>";
                    html = html.replace("nameString", resultList[i].replace(/_/g, " "));
                    listHTML = listHTML + html;
                }
                $("ul#results").html("");
                $("ul#results").html(listHTML);
            },
            error: ajaxFailed
        });
    }

    function ajaxFailed(xmlRequest) {
        console.log(xmlRequest.status + ' \n\r ' +
              xmlRequest.statusText + '\n\r' +
              xmlRequest.responseText);
    }
    
    //Bind key up on the text input to run the search function
    //Also sets a 100ms time delay before running search again
    $("input#searchBox").bind("keyup", function (e) {
        clearTimeout($.data(this, 'timer'));

        var searchQuery = $('input#searchBox').val();

        if (searchQuery == '') {
            $("ul#results").fadeOut();
            $('h4#resultsText').fadeOut();
        } else {
            $("ul#results").fadeIn();
            $('h4#resultsText').fadeIn();
            $(this).data('timer', setTimeout(search, 100));
        };
    });
});